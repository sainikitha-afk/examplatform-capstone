// TestDataBuilder.cs — factory methods that produce valid domain entities for tests.
//
// WHY THIS EXISTS:
//   Every test that needs a User, Exam, or Attempt would otherwise duplicate 8–12
//   lines of object initialisation. Centralising it here means:
//     a) Tests are shorter and easier to read.
//     b) When entity properties change, only this file needs updating.
//     c) Tests can override only the properties relevant to their scenario
//        (e.g., BuildCandidate(isActive: false) for the deactivated-user test).
//
// CONVENTION:
//   All builders return VALID default objects. Tests that need invalid state
//   override specific fields — everything else stays valid.

using ExamPlatform.Domain.Entities;
using ExamPlatform.Domain.Enums;

namespace ExamPlatform.BLL.Tests.Helpers
{
    public static class TestDataBuilder
    {
        // ─────────────── USERS ────────────────────────────────────────

        public static User BuildCandidate(int userId = 1, bool isActive = true) => new()
        {
            UserId = userId,
            FullName = "Test Candidate",
            Username = $"candidate{userId}",
            Email = $"candidate{userId}@test.com",
            // BCrypt hash of "password123" — valid bcrypt format, won't be used in real verification
            PasswordHash = "$2a$11$dummyHashForTestingOnly1234567890123456789012",
            Role = UserRole.Candidate,
            DateOfJoining = DateTime.UtcNow.AddDays(-30),
            IsActive = isActive
        };

        public static User BuildExaminer(int userId = 10, bool isActive = true) => new()
        {
            UserId = userId,
            FullName = "Test Examiner",
            Username = $"examiner{userId}",
            Email = $"examiner{userId}@test.com",
            PasswordHash = "$2a$11$dummyHashForTestingOnly1234567890123456789012",
            Role = UserRole.Examiner,
            DateOfJoining = DateTime.UtcNow.AddDays(-60),
            IsActive = isActive
        };

        public static User BuildAdmin(int userId = 99) => new()
        {
            UserId = userId,
            FullName = "Test Admin",
            Username = $"admin{userId}",
            Email = $"admin{userId}@test.com",
            PasswordHash = "$2a$11$dummyHashForTestingOnly1234567890123456789012",
            Role = UserRole.Admin,
            DateOfJoining = DateTime.UtcNow.AddDays(-90),
            IsActive = true
        };

        // ─────────────── EXAM CATEGORY ────────────────────────────────

        public static ExamCategory BuildCategory(int catId = 1, string name = "C# Programming") => new()
        {
            CatId = catId,
            CatName = name,
            Description = $"Test category: {name}"
        };

        // ─────────────── QUESTION OPTION ──────────────────────────────

        public static QuestionOption BuildOption(
            int optionId, int questionId, string text, bool isCorrect = false) => new()
        {
            OptionId = optionId,
            QuestionId = questionId,
            OptionText = text,
            IsCorrect = isCorrect
        };

        // Returns a valid set of 4 options with exactly one marked correct.
        public static List<QuestionOption> BuildFourOptions(int questionId) =>
        [
            BuildOption(questionId * 10 + 1, questionId, "Option A", isCorrect: true),
            BuildOption(questionId * 10 + 2, questionId, "Option B"),
            BuildOption(questionId * 10 + 3, questionId, "Option C"),
            BuildOption(questionId * 10 + 4, questionId, "Option D"),
        ];

        // Returns the tuple form that service methods accept.
        public static List<(string OptionText, bool IsCorrect)> BuildFourOptionTuples() =>
        [
            ("Option A", true),
            ("Option B", false),
            ("Option C", false),
            ("Option D", false),
        ];

        // ─────────────── QUESTION ─────────────────────────────────────

        public static Question BuildQuestion(
            int questionId = 1,
            int examinerId = 10,
            int catId = 1,
            string text = "What is dependency injection?",
            decimal marks = 10m,
            string difficulty = "Medium",
            bool isActive = true) => new()
        {
            QuestionId = questionId,
            ExaminerId = examinerId,
            CatId = catId,
            QuestionText = text,
            Marks = marks,
            Difficulty = difficulty,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            IsActive = isActive,
            Options = BuildFourOptions(questionId)
        };

        // ─────────────── EXAM ─────────────────────────────────────────

        public static Exam BuildDraftExam(
            int examId = 1,
            int examinerId = 10,
            int catId = 1,
            decimal totalMarks = 50m,
            decimal passingMarks = 30m) => new()
        {
            ExamId = examId,
            ExaminerId = examinerId,
            CatId = catId,
            Title = $"Test Exam {examId}",
            DurationMinutes = 60,
            TotalMarks = totalMarks,
            PassingMarks = passingMarks,
            StartTime = DateTime.UtcNow.AddHours(-1),  // already started (for attempt tests)
            EndTime = DateTime.UtcNow.AddHours(2),
            ExamStatus = ExamStatus.Draft,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        public static Exam BuildPublishedExam(
            int examId = 1,
            int examinerId = 10,
            decimal totalMarks = 50m,
            decimal passingMarks = 30m) => new()
        {
            ExamId = examId,
            ExaminerId = examinerId,
            CatId = 1,
            Title = $"Published Exam {examId}",
            DurationMinutes = 60,
            TotalMarks = totalMarks,
            PassingMarks = passingMarks,
            StartTime = DateTime.UtcNow.AddHours(-1),  // window open
            EndTime = DateTime.UtcNow.AddHours(2),
            ExamStatus = ExamStatus.Published,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            ExamQuestions = new List<ExamQuestion>()   // populated per-test as needed
        };

        // ─────────────── ATTEMPT ──────────────────────────────────────

        public static Attempt BuildInProgressAttempt(
            int attemptId = 1,
            int userId = 1,
            int examId = 1) => new()
        {
            AttemptId = attemptId,
            UserId = userId,
            ExamId = examId,
            StartedAt = DateTime.UtcNow.AddMinutes(-10), // 10 minutes elapsed
            AttemptStatus = AttemptStatus.InProgress,
            AttemptAnswers = new List<AttemptAnswer>()
        };

        public static Attempt BuildSubmittedAttempt(
            int attemptId = 1,
            int userId = 1,
            int examId = 1,
            decimal score = 40m,
            bool isPassed = true) => new()
        {
            AttemptId = attemptId,
            UserId = userId,
            ExamId = examId,
            StartedAt = DateTime.UtcNow.AddHours(-1),
            SubmittedAt = DateTime.UtcNow.AddMinutes(-5),
            AttemptStatus = AttemptStatus.Submitted,
            Score = score,
            IsPassed = isPassed,
            TimeTakenSeconds = 3300,
            AttemptAnswers = new List<AttemptAnswer>()
        };

        // ─────────────── CERTIFICATE ──────────────────────────────────

        public static Certificate BuildCertificate(
            int certId = 1,
            int userId = 1,
            int examId = 1,
            int attemptId = 1,
            CertificateStatus status = CertificateStatus.Issued) => new()
        {
            CertificateId = certId,
            CertificateCode = $"CERT-2026-{certId:D6}",
            UserId = userId,
            ExamId = examId,
            AttemptId = attemptId,
            Score = 40m,
            IssuedAt = DateTime.UtcNow.AddDays(-1),
            CertStatus = status
        };

        // ─────────────── NOTIFICATION ─────────────────────────────────

        public static Notification BuildNotification(
            int notifId = 1,
            int userId = 1,
            bool isRead = false) => new()
        {
            NotificationId = notifId,
            UserId = userId,
            NotifType = NotificationType.ResultPublished,
            Message = "Test notification message.",
            RelatedExamId = 1,
            IsRead = isRead,
            CreatedAt = DateTime.UtcNow.AddMinutes(-30)
        };
    }
}
