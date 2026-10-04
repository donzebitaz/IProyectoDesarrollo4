using TodoApi.Models;

namespace TodoApi.Services
{
    public class LogNotificador : INotificador
    {
        private readonly string _logFilePath;
        private static readonly SemaphoreSlim _semaphore = new(1, 1);

        public LogNotificador(IWebHostEnvironment environment)
        {
            _logFilePath = Path.Combine(environment.ContentRootPath, "notifications.log");
        }

        public async Task NotificarAsync(TodoItem todoItem)
        {
            var logEntry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}] Tarea vencida notificada - ID: {todoItem.Id}, Título: \"{todoItem.Title}\", Usuario: '{todoItem.OwnerId}', Fecha de vencimiento: {todoItem.DueDate:yyyy-MM-dd HH:mm:ss UTC}{Environment.NewLine}";

            await _semaphore.WaitAsync();
            try
            {
                await File.AppendAllTextAsync(_logFilePath, logEntry);
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
