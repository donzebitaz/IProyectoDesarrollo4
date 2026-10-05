using TodoApi.Models;

namespace TodoApi.Services
{
    public class LogNotifier : INotifier
    {
        private readonly string _logFilePath;
        private static readonly SemaphoreSlim _semaphore = new(1, 1);

        private readonly ILogger<LogNotifier> _logger;
        public LogNotifier(IWebHostEnvironment env, ILogger<LogNotifier> logger)
        {
            _logger = logger;
            _logFilePath = Path.Combine(env.ContentRootPath, "notifications.log");
        }

        public async Task NotifyAsync(TodoItem todoItem)
        {
            var logEntry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}] Overdue task notified - ID: {todoItem.Id}, Title: \"{todoItem.Title}\", User: '{todoItem.OwnerId}', Due date: {todoItem.DueDate:yyyy-MM-dd HH:mm:ss UTC}{Environment.NewLine}";

            _logger.LogWarning("Overdue task notified - ID: {Id}, Title: {Title}, User: {User}",
                todoItem.Id, todoItem.Title, todoItem.OwnerId);

            // Solo UNA escritura al archivo
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