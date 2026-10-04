using TodoApi.Models;

namespace TodoApi.Services
{
    public class LogNotifier : INotifier
    {
        private readonly string _logFilePath;
        private static readonly SemaphoreSlim _semaphore = new(1, 1);

        public LogNotifier(IWebHostEnvironment environment)
        {
            _logFilePath = Path.Combine(environment.ContentRootPath, "notifications.log");
        }

        public async Task NotifyAsync(TodoItem todoItem)
        {
            var logEntry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}] Overdue task notified - ID: {todoItem.Id}, Title: \"{todoItem.Title}\", User: '{todoItem.OwnerId}', Due date: {todoItem.DueDate:yyyy-MM-dd HH:mm:ss UTC}{Environment.NewLine}";

            // Only one write to the file at a time
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