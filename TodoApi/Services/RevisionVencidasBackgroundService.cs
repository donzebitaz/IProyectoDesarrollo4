namespace TodoApi.Services
{
    // Se ejecuta la revisión de tareas vencidas de forma constante y sin ninguna solicitud HTTP
    public class RevisionVencidasBackgroundService : BackgroundService
    {
        private const int IntervaloPorDefectoSegundos = 30;

        // Un BackgroundService es singleton pero IRevisionVencidasService es scoped. 
        // Al singleton se le inyecta el scope.
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RevisionVencidasBackgroundService> _logger;
        private readonly TimeSpan _intervalo;

        public RevisionVencidasBackgroundService(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ILogger<RevisionVencidasBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;

            var segundos = configuration.GetValue<int?>("Notificaciones:IntervaloRevisionSegundos")
                           ?? IntervaloPorDefectoSegundos;
            if (segundos < 1) segundos = IntervaloPorDefectoSegundos;

            _intervalo = TimeSpan.FromSeconds(segundos);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Overdue task review started. Interval: {Intervalo}.", _intervalo);

            try
            {
                using var timer = new PeriodicTimer(_intervalo);

                // la primera revisión corre apenas arranca la app y luego por cada periodo de tiempo
                do
                {
                    await RevisarAsync(stoppingToken);
                }
                while (await timer.WaitForNextTickAsync(stoppingToken));
            }
            catch (OperationCanceledException)
            {
                
            }

            _logger.LogInformation("Overdue task review stopped.");
        }

        private async Task RevisarAsync(CancellationToken stoppingToken)
        {
            try
            {
                // Scope nuevo por revisión
                using var scope = _scopeFactory.CreateScope();
                var servicio = scope.ServiceProvider.GetRequiredService<IRevisionVencidasService>();
                var notificadas = await servicio.RevisarYNotificarAsync(null, stoppingToken);

                if (notificadas > 0)
                {
                    _logger.LogInformation("Overdue task review notified {Count} task(s).", notificadas);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw; 
            }
            catch (Exception ex)
            {
                // En caso de fallar se registra y se reintenta en el próximo ciclo.
                _logger.LogError(ex, "Overdue task review failed. It will retry on the next interval.");
            }
        }
    }
}