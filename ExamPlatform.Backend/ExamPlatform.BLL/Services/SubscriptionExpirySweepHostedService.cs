// SubscriptionExpirySweepHostedService — periodically runs
// ExaminerSubscriptionService.SweepExpiredSubscriptionsAsync so
// ProfessionalActive -> GracePeriod -> FreeForever transitions happen even
// without a user-triggered action. Small and self-contained: its own hosted
// service, its own DI scope per tick (same IServiceScopeFactory pattern as
// CommonBackgroundService), no dependency on the AI Coach/analytics
// background queue.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ExamPlatform.BLL.Interfaces;

namespace ExamPlatform.BLL.Services
{
    public class SubscriptionExpirySweepHostedService : BackgroundService
    {
        private static readonly TimeSpan SweepInterval = TimeSpan.FromHours(1);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SubscriptionExpirySweepHostedService> _logger;

        public SubscriptionExpirySweepHostedService(
            IServiceScopeFactory scopeFactory,
            ILogger<SubscriptionExpirySweepHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(SweepInterval);

            while (!stoppingToken.IsCancellationRequested
                   && await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var subscriptionService = scope.ServiceProvider
                        .GetRequiredService<IExaminerSubscriptionService>();
                    var affected = await subscriptionService.SweepExpiredSubscriptionsAsync();
                    if (affected > 0)
                        _logger.LogInformation(
                            "Subscription expiry sweep transitioned {Count} subscription(s).", affected);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Subscription expiry sweep failed.");
                }
            }
        }
    }
}
