namespace TodoApi.Services
{
    // Corre la revision de tareas vencidas de forma continua, sin ninguna solicitud HTTP
    public class OverdueReviewBackgroundService : BackgroundService
    {
        private const int DefaultIntervalSeconds = 30;

        // Un BackgroundService es singleton pero IOverdueReviewService es scoped.
        // Se le inyecta el scope factory al singleton en su lugar.
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

                // La primera revision corre apenas arranca la app, luego una vez por intervalo
                do
                {
                    await ReviewAsync(stoppingToken);
                }
                while (await timer.WaitForNextTickAsync(stoppingToken));
            }
            catch (OperationCanceledException)
            {
                // Esperado cuando la aplicacion se esta cerrando
            }

            _logger.LogInformation("Overdue task review stopped.");
        }

        private async Task ReviewAsync(CancellationToken stoppingToken)
        {
            try
            {
                // Scope nuevo por cada revision
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
                // Si falla, se registra el error y se reintenta en el siguiente intervalo
                _logger.LogError(ex, "Overdue task review failed. It will retry on the next interval.");
            }
        }
    }
}