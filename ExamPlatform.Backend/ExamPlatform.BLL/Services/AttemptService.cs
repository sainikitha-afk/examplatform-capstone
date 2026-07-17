// AttemptService manages the candidate exam-taking experience:
// start/resume, answer saving, timer enforcement, submission, and result retrieval.
//
// Server-authoritative timer: the service computes remaining time as
//   max(0, durationMinutes × 60 − (UtcNow − startedAt).TotalSeconds)
// on every operation. If remainingSeconds reaches 0, the attempt is
// auto-submitted immediately — the server's clock, not the browser's.

// CA1848: standard ILogger extension methods used deliberately — see CertificateService.cs
#pragma warning disable CA1848
using Microsoft.Extensions.Logging;
using ExamPlatform.BLL.Exceptions;
using ExamPlatform.BLL.Interfaces;
using ExamPlatform.BLL.Models;
using ExamPlatform.DAL.Interfaces;
using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace ExamPlatform.BLL.Services
{
    public class AttemptService : IAttemptService
    {
        // ───────────── DEPENDENCIES ─────────────
        private readonly IAttemptRepository _attemptRepository;
        private readonly IAttemptAnswerRepository _answerRepository;
        private readonly IExamRepository _examRepository;

        // INotificationRepository: examiner alerts — a candidate resuming after a
        // disconnection or being auto-submitted on timeout is operationally
        // significant to the exam owner, so we drop an in-app notification.
        private readonly INotificationRepository _notificationRepository;

        // ILogger<AttemptService>: attempt events (start, auto-submit, manual submit)
        // are important operational signals — log them so ops can reconstruct
        // what a candidate experienced if they report an issue.
        private readonly ILogger<AttemptService> _logger;
        private readonly IBackgroundTaskQueue _taskQueue;

        // Premium-exam access gate (Part 4/9): a candidate may not start/resume
        // a brand-new attempt on an IsPremium exam without an Active purchase.
        private readonly ICandidatePurchaseRepository _purchaseRepository;

        public AttemptService(
            IAttemptRepository attemptRepository,
            IAttemptAnswerRepository answerRepository,
            IExamRepository examRepository,
            INotificationRepository notificationRepository,
            ILogger<AttemptService> logger,
            IBackgroundTaskQueue taskQueue,
            ICandidatePurchaseRepository purchaseRepository)
        {
            _attemptRepository = attemptRepository;
            _answerRepository = answerRepository;
            _examRepository = examRepository;
            _notificationRepository = notificationRepository;
            _logger = logger;
            _taskQueue = taskQueue;
            _purchaseRepository = purchaseRepository;
        }

        // ════════════════════════════════════════════════════════════════
        //   START OR RESUME
        // ════════════════════════════════════════════════════════════════
        /// <summary>
        /// Creates a new attempt or returns the existing InProgress one.
        /// The returned Attempt has Exam (with questions+options) populated.
        /// </summary>
        public async Task<Attempt> StartOrResumeAsync(int userId, int examId)
        {
            // ─── LOAD EXAM ────────────────────────────────────────────
            // We need the full question+options tree so the response includes
            // the exam questions for the candidate to answer.
            var exam = await _examRepository.GetByIdWithQuestionsAndOptionsAsync(examId);
            if (exam == null)
            {
                _logger.LogWarning(
                    "StartOrResume: exam {ExamId} not found for user {UserId}.", examId, userId);
                throw new ExamNotFoundException(examId);
            }

            // ─── VALIDATE EXAM STATUS ─────────────────────────────────
            // Status must be Published for both new starts AND resumes.
            // (UnpublishAsync already blocks if any attempts exist, so a
            // Published→Draft flip can't happen while someone is mid-attempt.)
            if (exam.ExamStatus != ExamStatus.Published)
            {
                _logger.LogWarning(
                    "User {UserId} tried to start exam {ExamId} but status is '{Status}'.",
                    userId, examId, exam.ExamStatus);
                throw new ExamNotOpenForAttemptException(
                    $"exam is in '{exam.ExamStatus}' status.");
            }

            // ─── PREMIUM ACCESS GATE (new attempts AND resumes) ──────
            // Re-checked server-side on every call — never trusted from a
            // cached frontend flag, and never skipped just because an
            // InProgress attempt already exists. Without this on the resume
            // path, a candidate whose purchase was refunded mid-attempt could
            // keep resuming, answering and submitting a premium exam they no
            // longer have access to.
            await EnsurePremiumAccessAsync(exam, userId);

            // ─── RESUME CHECK (BEFORE window check) ──────────────────
            // A candidate who started before EndTime must be able to resume
            // even after the clock passes EndTime (their server timer will
            // auto-submit them). The window guard below is only for NEW
            // attempts. Checking resume first avoids blocking a candidate
            // who is legitimately mid-exam but whose browser crashed.
            var existing = await _attemptRepository.GetInProgressAsync(userId, examId);
            if (existing != null)
            {
                _logger.LogInformation(
                    "User {UserId} resumed existing attempt {AttemptId} for exam {ExamId}.",
                    userId, existing.AttemptId, examId);

                // Load the FULL attempt (with saved answers + exam tree) so the
                // resume DTO correctly restores the candidate's previous selections.
                // Bug fix: the previous code returned `existing` with only
                // AttemptAnswers = new List<> (empty) — all answers appeared blank.
                var fullExisting = await _attemptRepository.GetByIdWithAnswersAndExamAsync(existing.AttemptId);

                // Auto-submit on resume if the timer already expired.
                // Normally GetByIdAsync handles this, but if the candidate hits
                // "Resume" as their first action after time-out, we must fire
                // here so the DB record is updated immediately.
                if (fullExisting!.AttemptStatus == AttemptStatus.InProgress)
                {
                    var remainingOnResume = ComputeRemainingSeconds(fullExisting);
                    if (remainingOnResume <= 0)
                    {
                        _logger.LogInformation(
                            "Attempt {AttemptId} timer expired on resume; auto-submitting for user {UserId}.",
                            fullExisting.AttemptId, userId);
                        await EvaluateAndSubmitAsync(fullExisting, AttemptStatus.AutoSubmitted);
                    }
                    else
                    {
                        // Alert the exam owner: a resume of an InProgress attempt means
                        // the candidate lost their session (browser crash, connection
                        // drop, page refresh) and reconnected mid-exam.
                        await NotifyExaminerAsync(
                            fullExisting,
                            NotificationType.AttemptResumed,
                            $"{fullExisting.User?.FullName ?? "A candidate"} reconnected and resumed their in-progress attempt for '{exam.Title}' (possible connection loss).");
                    }
                }

                return fullExisting;
            }

            // ─── ONE ATTEMPT PER CANDIDATE ────────────────────────────
            // Once a candidate has completed this exam (submitted or timed out),
            // it is no longer available to them. In-progress resumes were
            // handled above; this only blocks brand-new attempts.
            if (await _attemptRepository.HasCompletedAttemptAsync(userId, examId))
            {
                _logger.LogWarning(
                    "User {UserId} tried to re-attempt exam {ExamId} after completing it.",
                    userId, examId);
                throw new ExamNotOpenForAttemptException(
                    "you have already completed this exam. Each exam can only be taken once.");
            }

            // ─── WINDOW CHECK (new attempts only) ────────────────────
            // Only enforce start/end times when creating a brand-new attempt.
            // NOTE: a candidate who starts BEFORE EndTime always receives the
            // full duration — starting at 5:29 for a window ending 5:30 is
            // allowed, and the server timer lets them finish past 5:30.
            var now = DateTime.UtcNow;
            if (now < exam.StartTime)
            {
                _logger.LogWarning(
                    "User {UserId} tried to start exam {ExamId} before it opens (starts {StartTime:u}).",
                    userId, examId, exam.StartTime);
                throw new ExamNotOpenForAttemptException(
                    $"exam has not started yet (starts at {exam.StartTime:u}).");
            }
            if (now > exam.EndTime)
            {
                _logger.LogWarning(
                    "User {UserId} tried to start exam {ExamId} after it closed (ended {EndTime:u}).",
                    userId, examId, exam.EndTime);
                throw new ExamNotOpenForAttemptException(
                    "exam window has already closed.");
            }

            // ─── CREATE NEW ATTEMPT ───────────────────────────────────
            var attempt = new Attempt
            {
                UserId = userId,
                ExamId = examId,
                StartedAt = now,
                AttemptStatus = AttemptStatus.InProgress
                // Score, IsPassed, TimeTakenSeconds, SubmittedAt remain null
                // until the attempt is submitted and evaluated.
            };

            await _attemptRepository.AddAsync(attempt);
            await _attemptRepository.SaveChangesAsync();
            // EF Core back-fills AttemptId from the DB sequence after SaveChanges.

            _logger.LogInformation(
                "User {UserId} started new attempt {AttemptId} for exam {ExamId}.",
                userId, attempt.AttemptId, examId);

            // Attach the loaded exam so the controller can build the question list.
            attempt.Exam = exam;
            return attempt;
        }

        // ════════════════════════════════════════════════════════════════
        //   GET BY ID (resume view)
        // ════════════════════════════════════════════════════════════════
        /// <summary>
        /// Returns the attempt with answers and exam loaded.
        /// Auto-submits if the server timer has expired.
        /// </summary>
        public async Task<Attempt> GetByIdAsync(int attemptId, int userId)
        {
            var attempt = await _attemptRepository.GetByIdWithAnswersAndExamAsync(attemptId);

            if (attempt == null || attempt.UserId != userId)
            {
                _logger.LogWarning(
                    "GetById: attempt {AttemptId} not found or not owned by user {UserId}.",
                    attemptId, userId);
                throw new AttemptNotFoundException(attemptId);
            }

            // If already submitted, just return — no timer check needed.
            if (attempt.AttemptStatus != AttemptStatus.InProgress)
                return attempt;

            // ─── SERVER-AUTHORITATIVE TIMER CHECK ────────────────────
            var remaining = ComputeRemainingSeconds(attempt);
            if (remaining <= 0)
            {
                // Time is up — auto-submit immediately.
                _logger.LogInformation(
                    "Attempt {AttemptId} timer expired; auto-submitting for user {UserId}.",
                    attemptId, userId);
                await EvaluateAndSubmitAsync(attempt, AttemptStatus.AutoSubmitted);
            }

            return attempt;
        }

        // ════════════════════════════════════════════════════════════════
        //   SAVE ANSWER (auto-save)
        // ════════════════════════════════════════════════════════════════
        /// <summary>
        /// Upserts the candidate's answer for one question.
        /// Also enforces the timer — auto-submits if time is up.
        /// </summary>
        // ════════════════════════════════════════════════════════════════
        //   RECORD TAB SWITCH (proctoring / integrity)
        // ════════════════════════════════════════════════════════════════
        // The examiner is alerted once the candidate crosses this many tab
        // switches on a single attempt (not on every switch — that would spam
        // the inbox). Chosen at 3: one accidental switch is human; three is a
        // pattern worth a look.
        private const int TabSwitchAlertThreshold = 3;

        /// <summary>
        /// Increments the attempt's tab-switch counter and alerts the examiner
        /// once the threshold is crossed. Ownership-checked. Silently no-ops for
        /// attempts that are no longer InProgress so a late browser event after
        /// submit can't error the candidate.
        /// </summary>
        public async Task<int> RecordTabSwitchAsync(int attemptId, int userId)
        {
            var attempt = await _attemptRepository.GetByIdWithAnswersAndExamAsync(attemptId);

            if (attempt == null || attempt.UserId != userId)
            {
                _logger.LogWarning(
                    "RecordTabSwitch: attempt {AttemptId} not found or not owned by user {UserId}.",
                    attemptId, userId);
                throw new AttemptNotFoundException(attemptId);
            }

            // Not InProgress (already submitted / auto-submitted): nothing to
            // record. Return the stored count without error — a stray blur event
            // firing right after submit must not surface as a failure.
            if (attempt.AttemptStatus != AttemptStatus.InProgress)
                return attempt.TabSwitchCount;

            attempt.TabSwitchCount += 1;
            _attemptRepository.Update(attempt);
            await _attemptRepository.SaveChangesAsync();

            _logger.LogInformation(
                "RecordTabSwitch: attempt {AttemptId} now has {Count} tab switch(es) for user {UserId}.",
                attemptId, attempt.TabSwitchCount, userId);

            // Alert the examiner exactly once, on the switch that crosses the
            // threshold. Using '==' (not '>=') means one notification per attempt.
            if (attempt.TabSwitchCount == TabSwitchAlertThreshold)
            {
                await NotifyExaminerAsync(
                    attempt,
                    NotificationType.IntegrityFlag,
                    $"{attempt.User?.FullName ?? "A candidate"} left the exam tab {attempt.TabSwitchCount} times while taking '{attempt.Exam!.Title}'. Review their attempt for possible malpractice.");
            }

            return attempt.TabSwitchCount;
        }

        public async Task SaveAnswerAsync(
            int attemptId, int questionId, int? selectedOptionId, int userId)
        {
            var attempt = await _attemptRepository.GetByIdWithAnswersAndExamAsync(attemptId);

            if (attempt == null || attempt.UserId != userId)
            {
                _logger.LogWarning(
                    "SaveAnswer: attempt {AttemptId} not found or not owned by user {UserId}.",
                    attemptId, userId);
                throw new AttemptNotFoundException(attemptId);
            }

            if (attempt.AttemptStatus != AttemptStatus.InProgress)
            {
                _logger.LogWarning(
                    "SaveAnswer: attempt {AttemptId} is already submitted (status={Status}).",
                    attemptId, attempt.AttemptStatus);
                throw new AttemptAlreadySubmittedException(attemptId);
            }

            // Re-checked on every save — a refund revokes access mid-attempt.
            await EnsurePremiumAccessAsync(attempt.Exam!, userId);

            // ─── TIMER CHECK BEFORE SAVING ────────────────────────────
            var remaining = ComputeRemainingSeconds(attempt);
            if (remaining <= 0)
            {
                // Auto-submit: no point saving the answer — time is up.
                _logger.LogInformation(
                    "SaveAnswer: timer expired on attempt {AttemptId}; auto-submitting for user {UserId}.",
                    attemptId, userId);
                await EvaluateAndSubmitAsync(attempt, AttemptStatus.AutoSubmitted);
                return;
            }

            // ─── VALIDATE QUESTION BELONGS TO THIS EXAM ──────────────
            // GetByIdWithAnswersAndExamAsync already loaded Exam.ExamQuestions
            // with each Question and its Options — no extra DB round-trip.
            // Without this check a client could save answers for arbitrary
            // question IDs. Those rogue rows would never match an ExamQuestion
            // during evaluation, silently scoring 0 for every such answer.
            var examQuestion = attempt.Exam!.ExamQuestions
                .FirstOrDefault(eq => eq.QuestionId == questionId);
            if (examQuestion == null)
            {
                _logger.LogWarning(
                    "SaveAnswer: question {QuestionId} is not part of exam {ExamId} (attempt {AttemptId}).",
                    questionId, attempt.ExamId, attemptId);
                throw new ValidationException(
                    $"Question {questionId} is not part of exam {attempt.ExamId}.");
            }

            // ─── VALIDATE OPTION BELONGS TO THIS QUESTION ────────────
            // Root cause of Riya's score=0: selectedOptionId=145 was saved for
            // question 16 whose options are {61,62,63,64}. During evaluation
            // 145 never equals the correct option ID → isCorrect=false → 0 marks.
            // This guard ensures only the question's own options can be selected.
            if (selectedOptionId.HasValue)
            {
                var isValidOption = examQuestion.Question.Options
                    .Any(o => o.OptionId == selectedOptionId.Value);
                if (!isValidOption)
                {
                    _logger.LogWarning(
                        "SaveAnswer: option {OptionId} does not belong to question {QuestionId} (attempt {AttemptId}).",
                        selectedOptionId.Value, questionId, attemptId);
                    throw new ValidationException(
                        $"Option {selectedOptionId.Value} does not belong to question {questionId}.");
                }
            }

            // ─── UPSERT ANSWER ────────────────────────────────────────
            // UpsertAsync handles the insert-or-update and calls SaveChangesAsync.
            await _answerRepository.UpsertAsync(attemptId, questionId, selectedOptionId);
        }

        // ════════════════════════════════════════════════════════════════
        //   SUBMIT (manual)
        // ════════════════════════════════════════════════════════════════
        public async Task<Attempt> SubmitAsync(int attemptId, int userId)
        {
            var attempt = await _attemptRepository.GetByIdWithAnswersAndExamAsync(attemptId);

            if (attempt == null || attempt.UserId != userId)
            {
                _logger.LogWarning(
                    "Submit: attempt {AttemptId} not found or not owned by user {UserId}.",
                    attemptId, userId);
                throw new AttemptNotFoundException(attemptId);
            }

            if (attempt.AttemptStatus != AttemptStatus.InProgress)
            {
                _logger.LogWarning(
                    "Submit: attempt {AttemptId} already submitted (status={Status}).",
                    attemptId, attempt.AttemptStatus);
                throw new AttemptAlreadySubmittedException(attemptId);
            }

            // Re-checked on every submit — a refund revokes access mid-attempt.
            await EnsurePremiumAccessAsync(attempt.Exam!, userId);

            await EvaluateAndSubmitAsync(attempt, AttemptStatus.Submitted);

            _logger.LogInformation(
                "Attempt {AttemptId} manually submitted by user {UserId}; score={Score}.",
                attemptId, userId, attempt.Score);

            // Add the attempt to the background queue for AI categorization & chart generation
            // This is fire-and-forget, so the user doesn't wait!
            if (_taskQueue != null)
            {
                _taskQueue.QueueBackgroundWorkItem(async (sp, ct) =>
                {
                    try
                    {
                        var processor = sp.GetRequiredService<IAnalyticsProcessor>();
                        await processor.ProcessAttemptAsync(attemptId, ct);
                    }
                    catch (Exception ex)
                    {
                        var logger = sp.GetRequiredService<ILogger<AttemptService>>();
                        logger.LogError(ex, "Background analytics failed for attempt {AttemptId}. Retrying in 10 minutes...", attemptId);
                        
                        // Retry logic
                        _ = Task.Run(async () =>
                        {
                            await Task.Delay(TimeSpan.FromMinutes(10), ct);
                            _taskQueue.QueueBackgroundWorkItem(async (innerSp, innerCt) => 
                            {
                                var retryProcessor = innerSp.GetRequiredService<IAnalyticsProcessor>();
                                await retryProcessor.ProcessAttemptAsync(attemptId, innerCt);
                            });
                        }, ct);
                    }
                });
            }

            return attempt;
        }

        // ════════════════════════════════════════════════════════════════
        //   GET BY USER (history)
        // ════════════════════════════════════════════════════════════════
        public async Task<IEnumerable<Attempt>> GetByUserAsync(int userId)
        {
            return await _attemptRepository.GetByUserAsync(userId);
        }

        // ════════════════════════════════════════════════════════════════
        //   GET BY USER PAGED
        // ════════════════════════════════════════════════════════════════
        public async Task<PagedResult<Attempt>> GetByUserPagedAsync(
            int userId, int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            _logger.LogInformation(
                "User {UserId} retrieving attempt history page {Page} (pageSize={PageSize}).",
                userId, page, pageSize);

            var (items, totalCount) =
                await _attemptRepository.GetByUserPagedAsync(userId, page, pageSize);

            return new PagedResult<Attempt>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        // ════════════════════════════════════════════════════════════════
        //   GET BY EXAM (examiner monitoring)
        // ════════════════════════════════════════════════════════════════
        /// <summary>
        /// Returns every attempt (all statuses) for an exam so the examiner can
        /// monitor who took it, live progress, disconnections, and outcomes.
        /// Examiners may only view their own exams; admins may view any.
        /// </summary>
        public async Task<IEnumerable<Attempt>> GetAllByExamForExaminerAsync(
            int examId, int requesterId, bool isAdmin)
        {
            var exam = await _examRepository.GetByIdAsync(examId);
            if (exam == null)
            {
                _logger.LogWarning(
                    "GetAllByExam: exam {ExamId} not found (requester {UserId}).", examId, requesterId);
                throw new ExamNotFoundException(examId);
            }

            // Ownership rule: hide other examiners' exams entirely (404, not 403,
            // to avoid leaking exam existence) — same convention as ExamService.
            if (!isAdmin && exam.ExaminerId != requesterId)
            {
                _logger.LogWarning(
                    "GetAllByExam: examiner {UserId} does not own exam {ExamId}.", requesterId, examId);
                throw new ExamNotFoundException(examId);
            }

            return await _attemptRepository.GetAllByExamIdAsync(examId);
        }

        // ════════════════════════════════════════════════════════════════
        //   GET RESULT
        // ════════════════════════════════════════════════════════════════
        /// <summary>
        /// Returns the candidate's own attempt for the result view.
        /// <para>
        /// BUSINESS RULE (revised): the score and pass/fail state are visible IMMEDIATELY
        /// after submission — MCQ scoring is fully automatic, so there is nothing to wait
        /// for. What remains gated on the examiner publishing results is decided at the
        /// API layer: the per-question review (which exposes correct answers — an exam-
        /// integrity risk while other candidates may still be sitting the exam), the topic
        /// chart, analytics, AI coaching, and certificates.
        /// </para>
        /// </summary>
        public async Task<Attempt> GetResultAsync(int attemptId, int userId)
        {
            var attempt = await _attemptRepository.GetByIdWithAnswersAndExamAsync(attemptId);

            if (attempt == null || attempt.UserId != userId)
            {
                _logger.LogWarning(
                    "GetResult: attempt {AttemptId} not found or not owned by user {UserId}.",
                    attemptId, userId);
                throw new AttemptNotFoundException(attemptId);
            }

            // An InProgress attempt has no score yet — there is no result to show, and
            // exposing the questions through this endpoint would bypass the attempt flow.
            if (attempt.AttemptStatus == AttemptStatus.InProgress)
            {
                _logger.LogWarning(
                    "GetResult: user {UserId} requested result for attempt {AttemptId} which is still in progress.",
                    userId, attemptId);
                throw new ResultsNotPublishedException("This attempt has not been submitted yet.");
            }

            return attempt;
        }

        // ═══════════════════════════════════════════════════════════════
        //   PRIVATE HELPERS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Throws PremiumAccessRequiredException if <paramref name="exam"/> is
        /// premium and the candidate does not currently hold an Active purchase.
        /// Called on every code path that can advance an InProgress attempt
        /// (start, resume, save-answer, submit) — not just the initial start —
        /// so a purchase refunded mid-attempt immediately cuts off further
        /// progress instead of only blocking a brand-new attempt.
        /// </summary>
        private async Task EnsurePremiumAccessAsync(Exam exam, int userId)
        {
            if (!exam.IsPremium) return;

            var purchase = await _purchaseRepository.GetByUserAndExamAsync(userId, exam.ExamId);
            if (purchase == null || purchase.Status != CandidatePurchaseStatus.Active)
            {
                _logger.LogWarning(
                    "User {UserId} attempted premium exam {ExamId} without an active purchase.",
                    userId, exam.ExamId);
                throw new PremiumAccessRequiredException(exam.ExamId, exam.Price ?? 0m);
            }
        }

        /// <summary>
        /// Computes how many seconds remain on the server-authoritative timer.
        /// Returns 0 (not negative) when time has expired.
        /// </summary>
        private static double ComputeRemainingSeconds(Attempt attempt)
        {
            // durationSeconds = total allowed time for this exam.
            // elapsed = how long the candidate has been working since StartedAt.
            // remaining = max(0, allowed − elapsed) ensures we never return negative.
            var durationSeconds = attempt.Exam!.DurationMinutes * 60.0;
            var elapsed = (DateTime.UtcNow - attempt.StartedAt).TotalSeconds;
            return Math.Max(0, durationSeconds - elapsed);
        }

        /// <summary>
        /// Evaluates all questions, calculates score, and marks the attempt
        /// as Submitted or AutoSubmitted. Shared by manual and auto-submit paths.
        /// </summary>
        private async Task EvaluateAndSubmitAsync(Attempt attempt, AttemptStatus finalStatus)
        {
            // ─── LOAD FULL EXAM DATA ──────────────────────────────────
            // GetByIdWithAnswersAndExamAsync already loaded Exam.ExamQuestions.Question.Options,
            // so we have everything needed for evaluation in memory.
            var examQuestions = attempt.Exam!.ExamQuestions
                .OrderBy(eq => eq.QuestionOrder)
                .ToList();

            // ─── LOAD ANSWERS ─────────────────────────────────────────
            // Also already loaded via the include chain — access via navigation.
            var answers = attempt.AttemptAnswers.ToList();

            decimal totalScore = 0m;

            foreach (var eq in examQuestions)
            {
                var question = eq.Question;
                // Find the correct option for this question.
                var correctOption = question.Options.FirstOrDefault(o => o.IsCorrect);

                // Find the candidate's answer row (null = question was skipped).
                var answerRow = answers.FirstOrDefault(a => a.QuestionId == question.QuestionId);

                if (answerRow == null)
                {
                    // Skipped question — no row was saved via auto-save.
                    // No marks awarded; nothing to update.
                    continue;
                }

                // Determine if the candidate's choice was correct.
                bool isCorrect = correctOption != null &&
                                 answerRow.SelectedOptionId == correctOption.OptionId;

                decimal marksAwarded = isCorrect ? question.Marks : 0m;

                // Update the answer row with the evaluation result.
                answerRow.IsCorrect = isCorrect;
                answerRow.MarksAwarded = marksAwarded;
                _answerRepository.Update(answerRow); // queues UPDATE in change tracker

                totalScore += marksAwarded;
            }

            // ─── UPDATE ATTEMPT ───────────────────────────────────────
            attempt.Score = totalScore;
            attempt.IsPassed = totalScore >= attempt.Exam.PassingMarks;
            attempt.TimeTakenSeconds = (int)(DateTime.UtcNow - attempt.StartedAt).TotalSeconds;
            attempt.SubmittedAt = DateTime.UtcNow;
            attempt.AttemptStatus = finalStatus; // Submitted or AutoSubmitted

            _attemptRepository.Update(attempt);

            // Single SaveChangesAsync commits: all updated answer rows + attempt.
            // EF Core wraps them in one implicit database transaction.
            await _attemptRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Attempt {AttemptId} evaluated: status={Status}, score={Score}, passed={IsPassed}.",
                attempt.AttemptId, finalStatus, totalScore, attempt.IsPassed);

            // Alert the exam owner when an attempt is force-closed by the server
            // timer — a strong signal the candidate disconnected and never returned.
            if (finalStatus == AttemptStatus.AutoSubmitted)
            {
                await NotifyExaminerAsync(
                    attempt,
                    NotificationType.AttemptAutoSubmitted,
                    $"{attempt.User?.FullName ?? "A candidate"}'s attempt for '{attempt.Exam!.Title}' was auto-submitted when time expired (score: {totalScore}).");
            }
        }

        /// <summary>
        /// Creates an in-app notification for the examiner who owns the attempt's exam.
        /// Failures are logged, never thrown — an alert must not break the candidate flow.
        /// </summary>
        private async Task NotifyExaminerAsync(Attempt attempt, NotificationType type, string message)
        {
            try
            {
                await _notificationRepository.AddAsync(new Notification
                {
                    UserId = attempt.Exam!.ExaminerId,
                    NotifType = type,
                    Message = message,
                    RelatedExamId = attempt.ExamId,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
                await _notificationRepository.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to create examiner notification ({Type}) for attempt {AttemptId}.",
                    type, attempt.AttemptId);
            }
        }
    }
}
