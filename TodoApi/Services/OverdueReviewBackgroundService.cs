namespace TodoApi.Services
{
    // Runs the overdue task review continuously, without any HTTP request
    public class OverdueReviewBackgroundService : BackgroundService
    {
        private const int DefaultIntervalSeconds = 30;

        // A BackgroundService is a singleton but IOverdueReviewService is scoped.
        // The scope factory is injected into the singleton instead.
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OverdueReviewBackgroundService> _logger;
        private readonly TimeSpan _interval;

        public OverdueReviewBackgroundService(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ILogger<OverdueReviewBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;

            var seconds = configuration.GetValue<int?>("Notifications:ReviewIntervalSeconds")
                          ?? DefaultIntervalSeconds;
            if (seconds < 1) seconds = DefaultIntervalSeconds;

            _interval = TimeSpan.FromSeconds(seconds);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Overdue task review started. Interval: {Interval}.", _interval);

            try
            {
                using var timer = new PeriodicTimer(_interval);

                // The first review runs as soon as the app starts, then once per interval
                do
                {
                    await ReviewAsync(stoppingToken);
                }
                while (await timer.WaitForNextTickAsync(stoppingToken));
            }
            catch (OperationCanceledException)
            {
                // Expected when the application is shutting down
            }

            _logger.LogInformation("Overdue task review stopped.");
        }

        private async Task ReviewAsync(CancellationToken stoppingToken)
        {
            try
            {
                // New scope for each review
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IOverdueReviewService>();
                var notified = await service.ReviewAndNotifyAsync(null, stoppingToken);

                if (notified > 0)
                {
                    _logger.LogInformation("Overdue task review notified {Count} task(s).", notified);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // If it fails, the error is logged and the review is retried on the next interval
                _logger.LogError(ex, "Overdue task review failed. It will retry on the next interval.");
            }
        }
    }
}