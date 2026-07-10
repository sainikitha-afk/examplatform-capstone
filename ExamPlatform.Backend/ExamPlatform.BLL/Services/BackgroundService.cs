using System.Threading.Channels;
using ExamPlatform.BLL.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ExamPlatform.BLL.Services
{
    public class BackgroundTaskQueue : IBackgroundTaskQueue
    {
        private readonly Channel<Func<IServiceProvider, CancellationToken, Task>> _queue;

        public BackgroundTaskQueue(int capacity = 100)
        {
            var options = new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait
            };
            _queue = Channel.CreateBounded<Func<IServiceProvider, CancellationToken, Task>>(options);
        }

        public void QueueBackgroundWorkItem(Func<IServiceProvider, CancellationToken, Task> workItem)
        {
            if (workItem == null) throw new ArgumentNullException(nameof(workItem));
            _queue.Writer.TryWrite(workItem);
        }

        public async Task<Func<IServiceProvider, CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken)
        {
            return await _queue.Reader.ReadAsync(cancellationToken);
        }
    }

    // Using alias to avoid conflict with Microsoft.Extensions.Hosting.BackgroundService
    public class CommonBackgroundService : Microsoft.Extensions.Hosting.BackgroundService
    {
        private readonly IBackgroundTaskQueue _taskQueue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<CommonBackgroundService> _logger;

        public CommonBackgroundService(IBackgroundTaskQueue taskQueue, IServiceScopeFactory scopeFactory, ILogger<CommonBackgroundService> logger)
        {
            _taskQueue = taskQueue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Common Background Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                var workItem = await _taskQueue.DequeueAsync(stoppingToken);

                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    await workItem(scope.ServiceProvider, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing background work item.");
                }
            }
        }
    }
}
