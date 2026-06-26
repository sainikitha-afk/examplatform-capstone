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

namespace ExamPlatform.BLL.Services
{
    public class AttemptService : IAttemptService
    {
        // ───────────── DEPENDENCIES ─────────────
        private readonly IAttemptRepository _attemptRepository;
        private readonly IAttemptAnswerRepository _answerRepository;
        private readonly IExamRepository _examRepository;

        // ILogger<AttemptService>: attempt events (start, auto-submit, manual submit)
        // are important operational signals — log them so ops can reconstruct
        // what a candidate experienced if they report an issue.
        private readonly ILogger<AttemptService> _logger;

        public AttemptService(
            IAttemptRepository attemptRepository,
            IAttemptAnswerRepository answerRepository,
            IExamRepository examRepository,
            ILogger<AttemptService> logger)
        {
            _attemptRepository = attemptRepository;
            _answerRepository = answerRepository;
            _examRepository = examRepository;
            _logger = logger;
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
                }

                return fullExisting;
            }

            // ─── WINDOW CHECK (new attempts only) ────────────────────
            // Only enforce start/end times when creating a brand-new attempt.
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

            await EvaluateAndSubmitAsync(attempt, AttemptStatus.Submitted);

            _logger.LogInformation(
                "Attempt {AttemptId} manually submitted by user {UserId}; score={Score}.",
                attemptId, userId, attempt.Score);

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
        //   GET RESULT
        // ════════════════════════════════════════════════════════════════
        /// <summary>
        /// Returns the attempt with full per-question breakdown.
        /// Only available once the exam is in ResultsPublished status.
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

            // Business rule: results are only visible after the examiner publishes them.
            if (attempt.Exam?.ExamStatus != ExamStatus.ResultsPublished)
            {
                _logger.LogWarning(
                    "GetResult: user {UserId} requested result for attempt {AttemptId} but results are not yet published (exam status={Status}).",
                    userId, attemptId, attempt.Exam?.ExamStatus);
                throw new ResultsNotPublishedException();
            }

            return attempt;
        }

        // ═══════════════════════════════════════════════════════════════
        //   PRIVATE HELPERS
        // ═══════════════════════════════════════════════════════════════

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
        }
    }
}
