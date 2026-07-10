namespace ExamPlatform.BLL.Interfaces
{
    public interface IAnalyticsProcessor
    {
        Task ProcessAttemptAsync(int attemptId, CancellationToken stoppingToken);
    }
}
