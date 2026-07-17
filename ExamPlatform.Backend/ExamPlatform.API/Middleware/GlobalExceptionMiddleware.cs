using System.Net;
using System.Text.Json;
using ExamPlatform.BLL.Exceptions;
// New exception types from Features A–C:

namespace ExamPlatform.API.Middleware
{
    // Global exception handler. Sits early in the pipeline so it wraps
    // every downstream component and translates thrown exceptions into
    // clean HTTP responses.
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // ORDER MATTERS: specific exception types FIRST, BusinessException
            // fallback LAST. Pattern matching in C# switches is order-dependent
            // (first match wins). Putting BusinessException before its subclasses
            // makes those subclass cases unreachable — the compiler will reject it.
            HttpStatusCode statusCode;
            string message;

            switch (exception)
            {
                // ─── 400 Bad Request ────────────────────────────────────
                case ValidationException:
                    statusCode = HttpStatusCode.BadRequest;
                    message = exception.Message;
                    break;

                // ─── 401 Unauthorized ───────────────────────────────────
                case InvalidCredentialsException:
                    statusCode = HttpStatusCode.Unauthorized;
                    message = exception.Message;
                    break;

                // ─── 403 Forbidden ──────────────────────────────────────
                case UserDeactivatedException:
                case CoachAccessDeniedException:    // AI Coach: personalized coaching is Candidate-only
                    statusCode = HttpStatusCode.Forbidden;
                    message = exception.Message;
                    break;

                // ─── 402 Payment Required ───────────────────────────────
                // Subscription & Payment System: the caller is blocked from an
                // action until they pay (or wait out a cooldown) — never a
                // generic 500. Both carry extra structured fields the frontend
                // needs to render the right dialog/CTA (see below).
                case TrialExhaustedException:
                case PremiumAccessRequiredException:
                    statusCode = HttpStatusCode.PaymentRequired;
                    message = exception.Message;
                    break;

                // ─── 429 Too Many Requests ──────────────────────────────
                // Free Forever's rolling cooldown is a deliberate rate limit,
                // not an error — reuses the same status code the AI Coach's
                // rate limiter uses for the same reason.
                case CooldownActiveException:
                    statusCode = HttpStatusCode.TooManyRequests;
                    message = exception.Message;
                    break;

                // ─── 400 Bad Request ────────────────────────────────────
                case InvalidCouponException:
                    statusCode = HttpStatusCode.BadRequest;
                    message = exception.Message;
                    break;

                // ─── 503 Service Unavailable ────────────────────────────
                // The AI Learning Coach could not reach / parse Gemini. This is deliberately
                // NOT a 500: nothing is broken in ExamHub. The candidate's exam data and their
                // existing analytics are untouched, and the message says exactly that so a
                // failed AI call never reads as lost results. The provider's status code, body
                // and our API key stay server-side — the client sees one fixed message.
                case AiCoachUnavailableException:
                    statusCode = HttpStatusCode.ServiceUnavailable;
                    message = exception.Message;
                    break;

                // ─── 404 Not Found ──────────────────────────────────────
                case UserNotFoundException:
                case CategoryNotFoundException:
                case CertificateNotFoundException:
                case QuestionNotFoundException:     // Feature A: question not found or not owned
                case ExamNotFoundException:         // Feature B: exam not found or not owned
                case AttemptNotFoundException:              // Feature C: attempt not found or not owned
                case ExaminerRequestNotFoundException:      // Examiner request not found
                case IssueReportNotFoundException:          // Candidate issue report not found or not owned
                case InstitutionNotFoundException:          // Institute verification request not found
                    statusCode = HttpStatusCode.NotFound;
                    message = exception.Message;
                    break;

                // ─── 400 Bad Request ────────────────────────────────────
                // ExamNotPublishableException carries a reason string explaining
                // which pre-condition failed (missing questions, marks mismatch, etc.)
                case ExamNotPublishableException:
                    statusCode = HttpStatusCode.BadRequest;
                    message = exception.Message;
                    break;

                // ─── 409 Conflict ───────────────────────────────────────
                case DuplicateEmailException:
                case DuplicateUsernameException:
                case DuplicateCategoryException:
                case CategoryInUseException:             // category still referenced by exams/questions
                case DuplicateQuestionException:        // Duplicate question text in same category
                case QuestionInUseException:            // Feature A: question used in Published exam
                case ExamNotInDraftException:           // Feature B: edit/delete on non-Draft exam
                case ExamAlreadyPublishedException:     // Feature B: exam already published / results published
                case ExamHasAttemptsException:          // Unpublish blocked: candidates have started attempts
                case ExamNotOpenForAttemptException:    // Feature C: exam not in window or not Published
                case AttemptAlreadySubmittedException:  // Feature C: double-submit guard
                case ResultsNotPublishedException:              // Feature C: result requested before publish
                case ExaminerRequestAlreadyProcessedException:  // approve/reject on already-processed request
                case DuplicatePurchaseException:                // candidate already purchased this exam
                    statusCode = HttpStatusCode.Conflict;
                    message = exception.Message;
                    break;

                // ─── Fallback BusinessException → 400 ──────────────────
                // MUST come AFTER all specific BusinessException subclasses,
                // otherwise the compiler complains about unreachable cases.
                case BusinessException:
                    statusCode = HttpStatusCode.BadRequest;
                    message = exception.Message;
                    break;

                // ─── 500 — unknown/infrastructure errors ───────────────
                default:
                    _logger.LogError(exception, "Unhandled exception occurred.");
                    statusCode = HttpStatusCode.InternalServerError;
                    message = "An unexpected error occurred. Please try again later.";
                    break;
            }

            context.Response.StatusCode = (int)statusCode;
            context.Response.ContentType = "application/json";

            // A handful of Subscription & Payment System exceptions carry extra
            // structured data the frontend needs to render the right dialog —
            // which upgrade offer to show, when a cooldown lifts, which exam to
            // route to checkout. Every other exception keeps the plain
            // { status, error } shape unchanged.
            object payloadObject = exception switch
            {
                TrialExhaustedException ex => new
                {
                    status = (int)statusCode,
                    error = message,
                    upgradeDeclineCount = ex.UpgradeDeclineCount,
                    trialExamsUsed = ex.TrialExamsUsed,
                    maxExamsLifetime = ex.MaxExamsLifetime
                },
                CooldownActiveException ex => new
                {
                    status = (int)statusCode,
                    error = message,
                    nextEligibleAt = ex.NextEligibleAt
                },
                PremiumAccessRequiredException ex => new
                {
                    status = (int)statusCode,
                    error = message,
                    examId = ex.ExamId,
                    price = ex.Price
                },
                _ => new { status = (int)statusCode, error = message }
            };

            var payload = JsonSerializer.Serialize(payloadObject);

            await context.Response.WriteAsync(payload);
        }
    }
}