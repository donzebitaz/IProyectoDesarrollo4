using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Models;

namespace TodoApi.Services
{
    public class OverdueReviewService : IOverdueReviewService
    {
        private static readonly SemaphoreSlim _lock = new(1, 1);

        private readonly TodoDbContext _context;
        private readonly INotifier _notifier;

        public OverdueReviewService(TodoDbContext context, INotifier notifier)
        {
            _context = context;
            _notifier = notifier;
        }

        public async Task<int> ReviewAndNotifyAsync(string? ownerId = null, CancellationToken cancellationToken = default)
        {
            await _lock.WaitAsync(cancellationToken);
            try
            {
                var now = DateTime.UtcNow;

                // Overdue tasks that have not reached a final status yet
                var query = _context.TodoItems.Where(t =>
                    t.Status != TodoStatus.Completed
                    && t.Status != TodoStatus.Canceled
                    && t.DueDate != null
                    && t.DueDate < now
                    // The same due date is never notified twice. If the due date was changed
                    // and the task becomes overdue again, DueDate > LastNotifiedDueDate
                    // and it is notified again
                    && (t.LastNotifiedDueDate == null || t.DueDate > t.LastNotifiedDueDate));

                // Manual and automatic trigger
                if (ownerId != null)
                {
                    query = query.Where(t => t.OwnerId == ownerId);
                }

                var overdueItems = await query.ToListAsync(cancellationToken);

                foreach (var item in overdueItems)
                {
                    await _notifier.NotifyAsync(item);
                    item.LastNotifiedDueDate = item.DueDate;
                }

                if (overdueItems.Count > 0)
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }

                return overdueItems.Count;
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}