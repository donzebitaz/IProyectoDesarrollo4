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

                // Tareas vencidas que todavia no estan en un estado final
                var query = _context.TodoItems.Where(t =>
                    t.Status != TodoStatus.Completada
                    && t.Status != TodoStatus.Cancelada
                    && t.DueDate != null
                    && t.DueDate < now
                    // No se notifica dos veces la misma fecha de vencimiento. Si la fecha se cambia
                    // y la tarea vuelve a vencer, DueDate > LastNotifiedDueDate y se notifica de nuevo
                    && (t.LastNotifiedDueDate == null || t.DueDate > t.LastNotifiedDueDate));

                // Disparo manual y automatico
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