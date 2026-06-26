// ExamService is the most complex service in this application. It orchestrates
// the full exam lifecycle: creation in Draft, editing, question attachment,
// publishing, and the publish-results flow that generates certificates and
// notifications inside an explicit database transaction.
//
// Architecture note: ExamService injects ExamPlatformContext directly for
// TRANSACTION ORCHESTRATION ONLY. All actual data operations still go through
// repository interfaces. This is justified because explicit transactions span
// multiple aggregate roots (Exam, Certificate, Notification) owned by separate
// repositories — there is no other clean way to wrap them in a single BEGIN/COMMIT
// without either breaking the repository abstraction or adding transaction
// primitives to the generic IRepository<K,T> contract.

// CA1848: standard ILogger extension methods used deliberately — see CertificateService.cs
#pragma warning disable CA1848
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ExamPlatform.BLL.Exceptions;
using ExamPlatform.BLL.Interfaces;
using ExamPlatform.BLL.Models;
using ExamPlatform.DAL.Contexts;
using ExamPlatform.DAL.Interfaces;
using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Enums;

namespace ExamPlatform.BLL.Services
{
    public class ExamService : IExamService
    {
        // ───────────── DEPENDENCIES ─────────────
        private readonly IExamRepository _examRepository;
        private readonly IExamQuestionRepository _examQuestionRepository;
        private readonly IQuestionRepository _questionRepository;
        private readonly IAttemptRepository _attemptRepository;
        private readonly ICertificateRepository _certRepository;
        private readonly INotificationRepository _notifRepository;
        private readonly INotificationEmailService _emailService;
        private readonly ExamPlatformContext _context; // for transaction only — see file header

        private readonly ILogger<ExamService> _logger;

        public ExamService(
            IExamRepository examRepository,
            IExamQuestionRepository examQuestionRepository,
            IQuestionRepository questionRepository,
            IAttemptRepository attemptRepository,
            ICertificateRepository certRepository,
            INotificationRepository notifRepository,
            INotificationEmailService emailService,
            ExamPlatformContext context,
            ILogger<ExamService> logger)
        {
            _examRepository = examRepository;
            _examQuestionRepository = examQuestionRepository;
            _questionRepository = questionRepository;
            _attemptRepository = attemptRepository;
            _certRepository = certRepository;
            _notifRepository = notifRepository;
            _emailService = emailService;
            _context = context;
            _logger = logger;
        }

        // ════════════════════════════════════════════════════════════════
        //   GET PUBLISHED
        // ════════════════════════════════════════════════════════════════
        public async Task<IEnumerable<Exam>> GetPublishedAsync(int? categoryId)
        {
            return await _examRepository.GetPublishedAsync(categoryId);
        }

        // ════════════════════════════════════════════════════════════════
        //   GET PUBLISHED PAGED
        // ════════════════════════════════════════════════════════════════
        public async Task<PagedResult<Exam>> GetPublishedPagedAsync(
            int? categoryId, int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            _logger.LogInformation(
                "GetPublishedPagedAsync: page {Page}, pageSize {PageSize}, categoryId {CatId}.",
                page, pageSize, categoryId);

            var (items, totalCount) =
                await _examRepository.GetPublishedPagedAsync(categoryId, page, pageSize);

            return new PagedResult<Exam>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        // ════════════════════════════════════════════════════════════════
        //   GET MINE (Examiner)
        // ════════════════════════════════════════════════════════════════
        public async Task<IEnumerable<Exam>> GetMineAsync(int examinerId)
        {
            return await _examRepository.GetByExaminerAsync(examinerId);
        }

        // ════════════════════════════════════════════════════════════════
        //   GET MINE PAGED (Examiner)
        // ════════════════════════════════════════════════════════════════
        public async Task<PagedResult<Exam>> GetMinePagedAsync(int examinerId, int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            _logger.LogInformation(
                "GetMinePagedAsync: examiner {ExaminerId}, page {Page}, pageSize {PageSize}.",
                examinerId, page, pageSize);

            var (items, totalCount) =
                await _examRepository.GetByExaminerPagedAsync(examinerId, page, pageSize);

            return new PagedResult<Exam>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        // ════════════════════════════════════════════════════════════════
        //   GET BY ID WITH QUESTIONS + OPTIONS
        // ════════════════════════════════════════════════════════════════
        public async Task<Exam> GetByIdWithQuestionsAndOptionsAsync(int examId)
        {
            var exam = await _examRepository.GetByIdWithQuestionsAndOptionsAsync(examId);
            if (exam == null)
                throw new ExamNotFoundException(examId);
            return exam;
        }

        // ════════════════════════════════════════════════════════════════
        //   CREATE
        // ════════════════════════════════════════════════════════════════
        public async Task<Exam> CreateAsync(
            int examinerId, int catId, string title, string? instructions,
            int durationMinutes, decimal totalMarks, decimal passingMarks,
            DateTime startTime, DateTime endTime)
        {
            // ─── GUARD CLAUSES ────────────────────────────────────────
            if (string.IsNullOrWhiteSpace(title))
                throw new ValidationException("Exam title is required.");
            if (durationMinutes <= 0)
                throw new ValidationException("Duration must be greater than 0.");
            if (totalMarks <= 0)
                throw new ValidationException("Total marks must be greater than 0.");
            if (passingMarks <= 0 || passingMarks > totalMarks)
                throw new ValidationException("Passing marks must be > 0 and ≤ total marks.");
            if (startTime >= endTime)
                throw new ValidationException("Start time must be before end time.");

            var exam = new Exam
            {
                ExaminerId = examinerId,
                CatId = catId,
                Title = title.Trim(),
                Instructions = instructions?.Trim(),
                DurationMinutes = durationMinutes,
                TotalMarks = totalMarks,
                PassingMarks = passingMarks,
                StartTime = startTime,
                EndTime = endTime,
                ExamStatus = ExamStatus.Draft, // new exams always start as Draft
                CreatedAt = DateTime.UtcNow
            };

            await _examRepository.AddAsync(exam);
            await _examRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Exam {ExamId} created by examiner {ExaminerId}: '{Title}'.",
                exam.ExamId, examinerId, exam.Title);

            return exam;
        }

        // ════════════════════════════════════════════════════════════════
        //   UPDATE
        // ════════════════════════════════════════════════════════════════
        public async Task<Exam> UpdateAsync(
            int examId, int examinerId, string title, string? instructions,
            int durationMinutes, decimal totalMarks, decimal passingMarks,
            DateTime startTime, DateTime endTime)
        {
            var exam = await LoadOwnedDraftAsync(examId, examinerId);

            // ─── GUARD CLAUSES ────────────────────────────────────────
            if (string.IsNullOrWhiteSpace(title))
                throw new ValidationException("Exam title is required.");
            if (durationMinutes <= 0)
                throw new ValidationException("Duration must be greater than 0.");
            if (totalMarks <= 0)
                throw new ValidationException("Total marks must be greater than 0.");
            if (passingMarks <= 0 || passingMarks > totalMarks)
                throw new ValidationException("Passing marks must be > 0 and ≤ total marks.");
            if (startTime >= endTime)
                throw new ValidationException("Start time must be before end time.");

            exam.Title = title.Trim();
            exam.Instructions = instructions?.Trim();
            exam.DurationMinutes = durationMinutes;
            exam.TotalMarks = totalMarks;
            exam.PassingMarks = passingMarks;
            exam.StartTime = startTime;
            exam.EndTime = endTime;

            _examRepository.Update(exam);
            await _examRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Exam {ExamId} updated by examiner {ExaminerId}: new title='{Title}'.",
                examId, examinerId, exam.Title);

            return exam;
        }

        // ════════════════════════════════════════════════════════════════
        //   DELETE
        // ════════════════════════════════════════════════════════════════
        public async Task DeleteAsync(int examId, int examinerId)
        {
            var exam = await LoadOwnedDraftAsync(examId, examinerId);

            // Hard delete — no soft-delete for exams; Draft exams have no candidates.
            _examRepository.Delete(exam);
            await _examRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Exam {ExamId} deleted by examiner {ExaminerId}.", examId, examinerId);
        }

        // ════════════════════════════════════════════════════════════════
        //   ATTACH QUESTIONS
        // ════════════════════════════════════════════════════════════════
        public async Task<Exam> AttachQuestionsAsync(
            int examId, IEnumerable<int> questionIds, int examinerId)
        {
            var exam = await LoadOwnedDraftAsync(examId, examinerId);
            var ids = questionIds.ToList();

            // Track the display order for each new question using the current count
            // of already-attached questions as the starting offset.
            var existingCount = (await _examQuestionRepository.GetByExamIdAsync(examId)).Count();

            int order = existingCount;
            foreach (var qid in ids)
            {
                // Validate the question exists and is active.
                var question = await _questionRepository.GetByIdAsync(qid);
                if (question == null || !question.IsActive)
                    throw new ValidationException($"Question {qid} does not exist or is inactive.");

                // Ownership guard: an examiner can only attach their OWN questions.
                // Business rule: the question bank is private to each examiner —
                // attaching another examiner's question would expose their content
                // inside this exam and give the attaching examiner visibility into
                // another person's question bank.
                if (question.ExaminerId != examinerId)
                    throw new ValidationException(
                        $"Question {qid} was not created by you. You can only attach your own questions.");

                // Skip duplicates — idempotent attach.
                if (await _examQuestionRepository.ExistsAsync(examId, qid))
                    continue;

                order++;
                await _examQuestionRepository.AddAsync(new ExamQuestion
                {
                    ExamId = examId,
                    QuestionId = qid,
                    QuestionOrder = order
                });
            }

            await _examQuestionRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Examiner {ExaminerId} attached {Count} question(s) to exam {ExamId}.",
                examinerId, ids.Count, examId);

            // Reload with questions so the caller gets a fully populated exam.
            return await _examRepository.GetByIdWithQuestionsAsync(examId)
                   ?? exam;
        }

        // ════════════════════════════════════════════════════════════════
        //   DETACH QUESTION
        // ════════════════════════════════════════════════════════════════
        public async Task<Exam> DetachQuestionAsync(int examId, int questionId, int examinerId)
        {
            var exam = await LoadOwnedDraftAsync(examId, examinerId);

            // DeleteByExamAndQuestionAsync handles the "not found" case silently
            // (idempotent detach). It calls SaveChangesAsync internally.
            await _examQuestionRepository.DeleteByExamAndQuestionAsync(examId, questionId);

            return await _examRepository.GetByIdWithQuestionsAsync(examId) ?? exam;
        }

        // ════════════════════════════════════════════════════════════════
        //   PUBLISH
        // ════════════════════════════════════════════════════════════════
        public async Task<Exam> PublishAsync(int examId, int examinerId)
        {
            // Load with questions to access each question's Marks.
            var exam = await _examRepository.GetByIdWithQuestionsAsync(examId);

            if (exam == null || exam.ExaminerId != examinerId)
                throw new ExamNotFoundException(examId);

            // Idempotency guard: only Draft exams can be published.
            if (exam.ExamStatus != ExamStatus.Draft)
                throw new ExamAlreadyPublishedException(
                    $"Exam is already in '{exam.ExamStatus}' status.");

            // ─── PUBLISH PRE-CONDITIONS ───────────────────────────────
            // Rule 1: must have at least one question.
            if (!exam.ExamQuestions.Any())
                throw new ExamNotPublishableException("the exam has no questions attached.");

            // Rule 2: TotalMarks must equal the sum of attached question marks.
            // This prevents examiners from setting misleading total marks.
            var computedTotal = exam.ExamQuestions.Sum(eq => eq.Question.Marks);
            if (exam.TotalMarks != computedTotal)
                throw new ExamNotPublishableException(
                    $"TotalMarks ({exam.TotalMarks}) does not equal " +
                    $"the sum of question marks ({computedTotal}). " +
                    "Update TotalMarks or adjust the attached questions.");

            // Rule 3: PassingMarks must not exceed TotalMarks.
            if (exam.PassingMarks > exam.TotalMarks)
                throw new ExamNotPublishableException(
                    $"PassingMarks ({exam.PassingMarks}) exceeds TotalMarks ({exam.TotalMarks}).");

            // Rule 4: start time must be before end time.
            if (exam.StartTime >= exam.EndTime)
                throw new ExamNotPublishableException("StartTime must be before EndTime.");

            // ─── TRANSITION ───────────────────────────────────────────
            exam.ExamStatus = ExamStatus.Published;
            _examRepository.Update(exam);
            await _examRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Exam {ExamId} published by examiner {ExaminerId}: '{Title}' ({QuestionCount} questions, {TotalMarks} total marks).",
                examId, examinerId, exam.Title, exam.ExamQuestions.Count, exam.TotalMarks);

            return exam;
        }

        // ════════════════════════════════════════════════════════════════
        //   PUBLISH RESULTS
        // ════════════════════════════════════════════════════════════════
        public async Task<Exam> PublishResultsAsync(int examId, int examinerId)
        {
            // ─── LOAD AND VALIDATE ────────────────────────────────────
            var exam = await _examRepository.GetByIdAsync(examId);
            if (exam == null || exam.ExaminerId != examinerId)
                throw new ExamNotFoundException(examId);

            if (exam.ExamStatus != ExamStatus.Published)
                throw new ExamAlreadyPublishedException(
                    exam.ExamStatus == ExamStatus.ResultsPublished
                        ? "Results have already been published for this exam."
                        : $"Exam is in '{exam.ExamStatus}' status. Only Published exams can have results published.");

            _logger.LogInformation(
                "Publishing results for exam {ExamId} by examiner {ExaminerId}.",
                examId, examinerId);

            // ─── OPEN EXPLICIT TRANSACTION ────────────────────────────
            // We use an explicit transaction because we span multiple SaveChangesAsync
            // calls (one per certificate code-generation loop, plus a final commit).
            // All of these must succeed together or rollback together.
            // _context is injected solely for this purpose — see file header comment.
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // ─── 1. TRANSITION EXAM STATUS ────────────────────────
                exam.ExamStatus = ExamStatus.ResultsPublished;
                _examRepository.Update(exam);

                // ─── 2. FIND BEST ATTEMPT PER CANDIDATE ───────────────
                // GetByExamIdAsync already filters to Submitted/AutoSubmitted
                // and eagerly loads the User navigation (for email + notification).
                var allAttempts = await _attemptRepository.GetByExamIdAsync(examId);

                // Group by candidate, pick the attempt with the highest score.
                // MaxBy is LINQ-to-Objects (in-memory) — we materialised the list above.
                var bestPerCandidate = allAttempts
                    .GroupBy(a => a.UserId)
                    .Select(g => g.MaxBy(a => a.Score ?? 0m)!)
                    .ToList();

                var year = DateTime.UtcNow.Year;

                // Tracks (userId → certCode) for candidates who receive a NEW certificate
                // during this publish. Used after the transaction commits to send the
                // "Certificate Available" email without re-querying the database.
                var newCertCodes = new Dictionary<int, string>();

                // ─── 3. CERTIFICATES + NOTIFICATIONS ─────────────────
                foreach (var attempt in bestPerCandidate)
                {
                    // ─── 3a. CERTIFICATE (only for passing attempts) ──
                    if ((attempt.Score ?? 0m) >= exam.PassingMarks)
                    {
                        // Guard: only issue if no certificate exists yet for this
                        // (user, exam) pair. The DB UNIQUE constraint backs this up,
                        // but we check first to avoid an exception.
                        if (!await _certRepository.ExistsForUserAndExamAsync(attempt.UserId, examId))
                        {
                            // Insert with a temporary code — we need the database-generated
                            // CertificateId before we can build the final CERT-YYYY-NNNNNN code.
                            var cert = new Certificate
                            {
                                CertificateCode = "TEMP", // placeholder; updated below
                                UserId = attempt.UserId,
                                ExamId = examId,
                                AttemptId = attempt.AttemptId,
                                Score = attempt.Score!.Value, // snapshot at issuance
                                IssuedAt = DateTime.UtcNow,
                                CertStatus = CertificateStatus.Issued
                            };

                            await _certRepository.AddAsync(cert);

                            // SaveChangesAsync here (inside the transaction) materialises
                            // the INSERT and back-fills CertificateId from the DB sequence.
                            // We need the Id to generate the human-readable code.
                            await _certRepository.SaveChangesAsync();

                            // Build the code now that we have the Id.
                            cert.CertificateCode = $"CERT-{year}-{cert.CertificateId:D6}";
                            _certRepository.Update(cert);
                            // The UPDATE will be flushed in the SaveChangesAsync at
                            // the end of the loop (step 4 below).

                            // Record this candidate so we can send a certificate email
                            // after the transaction commits (see step 6 below).
                            newCertCodes[attempt.UserId] = cert.CertificateCode;

                            _logger.LogInformation(
                                "Certificate '{CertCode}' issued to user {UserId} for exam {ExamId}.",
                                cert.CertificateCode, attempt.UserId, examId);

                            // ─── CERTIFICATE ISSUED NOTIFICATION ──────────────
                            // A second notification — separate from ResultPublished —
                            // tells the candidate their certificate is ready to view
                            // and download. Creating it here (inside the transaction)
                            // ensures it is only written when the certificate itself
                            // is successfully committed; a rollback undoes both.
                            await _notifRepository.AddAsync(new Notification
                            {
                                UserId = attempt.UserId,
                                NotifType = NotificationType.CertificateIssued,
                                Message = $"Congratulations! Your certificate for '{exam.Title}' is now available.",
                                RelatedExamId = examId,
                                IsRead = false,
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                    }

                    // ─── 3b. NOTIFICATION (for every candidate, pass or fail) ──
                    var message = (attempt.Score ?? 0m) >= exam.PassingMarks
                        ? $"Results for '{exam.Title}' are published. You passed with a score of {attempt.Score}."
                        : $"Results for '{exam.Title}' are published. Your score was {attempt.Score}.";

                    await _notifRepository.AddAsync(new Notification
                    {
                        UserId = attempt.UserId,
                        NotifType = NotificationType.ResultPublished,
                        Message = message,
                        RelatedExamId = examId,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                // ─── 4. FLUSH ALL PENDING CHANGES ────────────────────
                // This commits: exam status, certificate code updates, notification inserts.
                await _examRepository.SaveChangesAsync();

                // ─── 5. COMMIT TRANSACTION ────────────────────────────
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Results published for exam {ExamId}: {CandidateCount} candidates processed.",
                    examId, bestPerCandidate.Count);

                // ─── 6. FIRE-AND-FORGET EMAILS ────────────────────────
                // Emails are sent AFTER the transaction commits so a failed email
                // does not roll back the certificate/notification records.
                // Each email is individually try-catched — one failure doesn't
                // block the rest. The NoOp implementation logs instead of sending
                // when SMTP is not configured (safe for local dev environments).
                //
                // Three email types are sent per candidate where applicable:
                //   a) Result Published     → all candidates (pass + fail)
                //   b) Congratulations      → passing candidates only
                //   c) Certificate Available → candidates who received a new cert
                foreach (var attempt in bestPerCandidate)
                {
                    var candidateName = attempt.User.FullName ?? attempt.User.Username;
                    var isPassed      = (attempt.Score ?? 0m) >= exam.PassingMarks;

                    // ── a) Result Published — sent to every candidate ─────────
                    try
                    {
                        await _emailService.SendAsync(
                            attempt.User.Email,
                            $"Results Published — {exam.Title}",
                            EmailTemplates.ResultPublished(
                                candidateName, exam.Title, attempt.Score ?? 0m,
                                exam.TotalMarks, isPassed),
                            isHtml: true);
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogWarning(emailEx,
                            "Failed to send ResultPublished email to user {UserId}.", attempt.UserId);
                    }

                    if (isPassed)
                    {
                        // ── b) Congratulations — sent to passing candidates ───
                        try
                        {
                            await _emailService.SendAsync(
                                attempt.User.Email,
                                $"Congratulations! You Passed — {exam.Title}",
                                EmailTemplates.YouPassed(
                                    candidateName, exam.Title,
                                    attempt.Score!.Value, exam.TotalMarks),
                                isHtml: true);
                        }
                        catch (Exception emailEx)
                        {
                            _logger.LogWarning(emailEx,
                                "Failed to send YouPassed email to user {UserId}.", attempt.UserId);
                        }

                        // ── c) Certificate Available — sent if a new cert was issued ─
                        if (newCertCodes.TryGetValue(attempt.UserId, out var certCode))
                        {
                            try
                            {
                                await _emailService.SendAsync(
                                    attempt.User.Email,
                                    $"Your Certificate is Available — {exam.Title}",
                                    EmailTemplates.CertificateAvailable(
                                        candidateName, exam.Title, certCode),
                                    isHtml: true);
                            }
                            catch (Exception emailEx)
                            {
                                _logger.LogWarning(emailEx,
                                    "Failed to send CertificateAvailable email to user {UserId}.", attempt.UserId);
                            }
                        }
                    }
                }

                return exam;
            }
            catch
            {
                // Any exception during the transactional steps (3–5) triggers a rollback.
                // Rollback undoes ALL changes made since BeginTransactionAsync:
                //   - exam status change
                //   - certificate inserts + code updates
                //   - notification inserts
                // The database is left exactly as it was before the call.
                _logger.LogError(
                    "Transaction rolled back during result publishing for exam {ExamId}.", examId);
                await transaction.RollbackAsync();
                throw; // re-throw so GlobalExceptionMiddleware can map it to HTTP
            }
        }

        // ════════════════════════════════════════════════════════════════
        //   UNPUBLISH (Published → Draft)
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Reverts a Published exam back to Draft so the examiner can make corrections.
        /// Rejected if: the exam is not Published, results have already been published,
        /// or any candidate has started an attempt (any status).
        /// isAdmin=true lets an Admin act on any exam without the ownership check.
        /// </summary>
        public async Task<Exam> UnpublishAsync(int examId, int callerId, bool isAdmin)
        {
            // Load exam — no question/option tree needed; just the status fields.
            var exam = await _examRepository.GetByIdAsync(examId);

            // ─── OWNERSHIP CHECK ─────────────────────────────────────
            // Admins bypass ownership; examiners must own the exam.
            // Both "not found" and "wrong owner" return 404 to avoid leaking
            // information about exams owned by other examiners.
            if (exam == null || (!isAdmin && exam.ExaminerId != callerId))
                throw new ExamNotFoundException(examId);

            // ─── STATUS GUARD: must be Published ─────────────────────
            // ResultsPublished exams have issued certificates and notifications;
            // reverting them would create orphaned records — explicitly blocked.
            // Draft exams have nothing to revert.
            if (exam.ExamStatus == ExamStatus.ResultsPublished)
                throw new ExamAlreadyPublishedException(
                    "Cannot unpublish an exam whose results have already been published.");

            if (exam.ExamStatus != ExamStatus.Published)
                throw new ExamNotInDraftException(
                    $"Exam is in '{exam.ExamStatus}' status. Only Published exams can be reverted to Draft.");

            // ─── ATTEMPT GUARD ────────────────────────────────────────
            // Even a single InProgress attempt means a candidate is currently
            // sitting the exam. Reverting now would corrupt their session.
            // AnyAsync issues a SQL EXISTS — no rows are loaded.
            if (await _attemptRepository.HasAnyForExamAsync(examId))
                throw new ExamHasAttemptsException();

            // ─── TRANSITION ───────────────────────────────────────────
            exam.ExamStatus = ExamStatus.Draft;
            _examRepository.Update(exam);
            await _examRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Exam {ExamId} reverted to Draft by caller {CallerId} (isAdmin={IsAdmin}).",
                examId, callerId, isAdmin);

            return exam;
        }

        // ───────────── PRIVATE HELPERS ─────────────

        /// <summary>
        /// Loads an exam, verifies the examiner owns it, and verifies it is in Draft.
        /// Throws ExamNotFoundException or ExamNotInDraftException as appropriate.
        /// </summary>
        private async Task<Exam> LoadOwnedDraftAsync(int examId, int examinerId)
        {
            var exam = await _examRepository.GetByIdAsync(examId);

            // Same 404 response for "doesn't exist" and "belongs to another examiner" —
            // avoids leaking information about other examiners' exams.
            if (exam == null || exam.ExaminerId != examinerId)
                throw new ExamNotFoundException(examId);

            if (exam.ExamStatus != ExamStatus.Draft)
                throw new ExamNotInDraftException(examId);

            return exam;
        }
    }
}
