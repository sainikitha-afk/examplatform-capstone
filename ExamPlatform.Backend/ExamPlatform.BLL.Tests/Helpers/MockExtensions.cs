// MockExtensions.cs — helper extension methods for ILogger<T> verification.
//
// WHY THIS EXISTS:
//   Verifying that a specific log level was emitted requires the verbose 5-parameter
//   overload of Mock.Verify. Wrapping it in an extension method keeps individual tests
//   readable — one line instead of 6.
//
// HOW IT WORKS:
//   ILogger.Log<TState> is the single core method; every extension (LogInformation,
//   LogWarning, LogError, …) ultimately calls it. Moq intercepts this core method
//   and we assert on the LogLevel argument only — not the message text, because
//   message phrasing is fragile and changes without changing behaviour.

using Microsoft.Extensions.Logging;
using Moq;

namespace ExamPlatform.BLL.Tests.Helpers
{
    public static class MockExtensions
    {
        // ────────────────────────────────────────────────────────────────
        // VerifyLog<T> — asserts that at least one log call at the given
        // level was made, regardless of message content.
        //
        // It.IsAny<It.IsAnyType>() matches any TState (the structured-log
        // state object). The Func<It.IsAnyType, Exception?, string> is the
        // formatter delegate — we don't care about it.
        // ────────────────────────────────────────────────────────────────
        public static void VerifyLog<T>(
            this Mock<ILogger<T>> loggerMock,
            LogLevel level,
            Times times)
        {
            loggerMock.Verify(
                x => x.Log(
                    level,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),    // the structured log state
                    It.IsAny<Exception?>(),       // exception (may be null)
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                times);
        }

        // ────────────────────────────────────────────────────────────────
        // VerifyWarning / VerifyError — convenience wrappers so tests
        // can write:  loggerMock.VerifyWarning(Times.AtLeastOnce())
        // ────────────────────────────────────────────────────────────────
        public static void VerifyWarning<T>(this Mock<ILogger<T>> loggerMock, Times times)
            => loggerMock.VerifyLog(LogLevel.Warning, times);

        public static void VerifyError<T>(this Mock<ILogger<T>> loggerMock, Times times)
            => loggerMock.VerifyLog(LogLevel.Error, times);

        public static void VerifyInformation<T>(this Mock<ILogger<T>> loggerMock, Times times)
            => loggerMock.VerifyLog(LogLevel.Information, times);
    }
}
