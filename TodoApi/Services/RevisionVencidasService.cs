using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Models;

namespace TodoApi.Services
{
    public class RevisionVencidasService : IRevisionVencidasService
    {
        private static readonly SemaphoreSlim _candado = new(1, 1);

        private readonly TodoDbContext _context;
        private readonly INotificador _notificador;

        public RevisionVencidasService(TodoDbContext context, INotificador notificador)
        {
            _context = context;
            _notificador = notificador;
        }

        public async Task<int> RevisarYNotificarAsync(string? ownerId = null, CancellationToken cancellationToken = default)
        {
            await _candado.WaitAsync(cancellationToken);
            try
            {
                var now = DateTime.UtcNow;

                // Tareas vencidas que todavía no están en un estado final
                var query = _context.TodoItems.Where(t =>
                    t.Status != TodoStatus.Completada
                    && t.Status != TodoStatus.Cancelada
                    && t.DueDate != null
                    && t.DueDate < now
                    // No se notifica dos veces la misma fecha de vencimiento pero si la fecha se cambión
                    // y se vuelve a vencer, significa que DueDate > LastNotifiedDueDate y se vuelve a notificar
                    && (t.LastNotifiedDueDate == null || t.DueDate > t.LastNotifiedDueDate));

                // Disparo manual y automático
                if (ownerId != null)
                {
                    query = query.Where(t => t.OwnerId == ownerId);
                }

                var overdueItems = await query.ToListAsync(cancellationToken);

                foreach (var item in overdueItems)
                {
                    await _notificador.NotificarAsync(item);
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
                _candado.Release();
            }
        }
    }
}