using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using TodoApi.Services;

namespace TodoApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TodoItemsController : ControllerBase
    {
        private readonly TodoDbContext _context;
        private readonly IOverdueReviewService _overdueReviewService;

        public TodoItemsController(TodoDbContext context, IOverdueReviewService overdueReviewService)
        {
            _context = context;
            _overdueReviewService = overdueReviewService;
        }

        // Diccionario de estados permitidos
        private static readonly Dictionary<TodoStatus, TodoStatus[]> AllowedTransitions = new()
        {
            [TodoStatus.Pendiente] = new[] { TodoStatus.EnProgreso, TodoStatus.Cancelada },
            [TodoStatus.EnProgreso] = new[] { TodoStatus.Completada, TodoStatus.Cancelada },
            [TodoStatus.Completada] = Array.Empty<TodoStatus>(),
            [TodoStatus.Cancelada] = Array.Empty<TodoStatus>()
        };


        // Se hace uso de un helper para guardar las fechas en UTC.
        private static DateTime? ToUtc(DateTime? date)
        {
            if (!date.HasValue) return null;

            return date.Value.Kind switch
            {
                DateTimeKind.Utc => date.Value,                         // ya viene en UTC
                DateTimeKind.Local => date.Value.ToUniversalTime(),     // convertir a UTC
                _ => DateTime.SpecifyKind(date.Value, DateTimeKind.Utc) // sin zona se asume UTC
            };
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<TodoItem>> GetTodoItem(int id)
        {
            var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var todoItem = await _context.TodoItems
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == id);

            if (todoItem == null) return NotFound(); //404
            if (todoItem.OwnerId != ownerId) return NotFound();

            return Ok(todoItem); //200
        }

        [HttpPost]
        public async Task<ActionResult<TodoItem>> CreateTodoItem(TodoItem todoItem)
        {
            //validar si categoryid trae algo
            //en caso positivo validar que el categoryid es valido
            //en caso de que no sea valido retornar un BadRequest()
            if (todoItem.CategoryId.HasValue)
            {
                var categoryExists = await _context.Categories.AnyAsync(t => t.Id == todoItem.CategoryId);
                if (!categoryExists) return BadRequest("The specified category does not exist");
            }

            //fix pendiente: para que el cliente no decida el estado y toda tarea nueva este en "Pendiente".
            todoItem.Status = TodoStatus.Pendiente;
            todoItem.CompletedAt = null;

            // El servidor es quien decide el dueno, sin importar que venga dentro del body
            todoItem.OwnerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            // El servidor decide cuando se creo
            todoItem.CreatedAt = DateTime.UtcNow;

            todoItem.DueDate = ToUtc(todoItem.DueDate);

            _context.TodoItems.Add(todoItem);

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTodoItem), new { id = todoItem.Id }, todoItem); //201
        }

        [HttpPost("notificarvencidas")]
        public async Task<ActionResult> NotifyOverdue()
        {
            var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            // Solo las tareas vencidas del usuario autenticado
            var notified = await _overdueReviewService.ReviewAndNotifyAsync(ownerId);

            return Ok(new { count = notified }); //200
        }

        [HttpPatch("{id:int}/status")]
        public async Task<ActionResult<TodoItem>> UpdateStatus(int id, UpdateStatusDto dto)
        {
            var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var todoItem = await _context.TodoItems.FindAsync(id);
            if (todoItem == null) return NotFound();
            if (todoItem.OwnerId != ownerId) return NotFound();

            var currentStatus = todoItem.Status;

            if (!AllowedTransitions[currentStatus].Contains(dto.Status))
            {
                return BadRequest($"Cannot transition from {currentStatus} to {dto.Status}.");
            }

            if (dto.Status == TodoStatus.Completada && todoItem.DueDate.HasValue && todoItem.DueDate.Value < DateTime.UtcNow
               && !dto.Force)
            {
                return BadRequest("The task is overdue. Set \"force\": true to complete it anyway.");
            }

            todoItem.Status = dto.Status;
            if (dto.Status == TodoStatus.Completada)
            {
                todoItem.CompletedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Ok(todoItem); //200
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TodoItem>>> GetTodoItems([FromQuery] TodoStatus? status, [FromQuery] int? categoryId, [FromQuery] bool? overdue)
        {
            var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var query = _context.TodoItems.Include(t => t.Category)
                .Where(t => t.OwnerId == ownerId)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(t => t.Status == status.Value);
            }

            if (categoryId.HasValue)
            {
                query = query.Where(t => t.CategoryId == categoryId.Value);
            }

            // solo se muestran las tareas vencidas que todavia no estan en un estado final.
            if (overdue == true)
            {
                var now = DateTime.UtcNow;
                query = query.Where(t => t.DueDate != null && t.DueDate < now && t.Status != TodoStatus.Completada
                    && t.Status != TodoStatus.Cancelada);
            }

            return Ok(await query.ToListAsync()); //200
        }

        // Estadisticas de tareas
        [HttpGet("stats")]
        public async Task<ActionResult<TodoStatsDto>> GetStats()
        {
            var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var now = DateTime.UtcNow;

            // Solo las tareas del usuario autenticado
            var tasks = await _context.TodoItems
                .AsNoTracking()
                .Where(t => t.OwnerId == ownerId)
                .ToListAsync();

            // LINQ, se agrupa por estado y se cuenta cada grupo
            var countByStatus = tasks
                .GroupBy(t => t.Status)
                .ToDictionary(g => g.Key, g => g.Count());

            // LINQ, cuenta con una condicion
            var overdueCount = tasks.Count(t =>
                t.DueDate != null
                && t.DueDate < now
                && t.Status != TodoStatus.Completada
                && t.Status != TodoStatus.Cancelada);

            // TimeSpan, duracion entre la creacion y el cierre de cada tarea completada
            List<TimeSpan> durations = tasks
                .Where(t => t.Status == TodoStatus.Completada && t.CompletedAt != null)
                .Select(t => t.CompletedAt!.Value - t.CreatedAt)
                .ToList();

            // Promedio en dias (nulo si no hay ninguna completada)
            double? averageDays = null;
            if (durations.Count > 0)
            {
                averageDays = Math.Round(durations.Average(d => d.TotalDays), 2);
            }

            var stats = new TodoStatsDto
            {
                TotalTasks = tasks.Count,

                // Si un estado no tiene tareas no aparece en el diccionario, por eso se usa 0
                Pending = countByStatus.GetValueOrDefault(TodoStatus.Pendiente, 0),
                InProgress = countByStatus.GetValueOrDefault(TodoStatus.EnProgreso, 0),
                Completed = countByStatus.GetValueOrDefault(TodoStatus.Completada, 0),
                Canceled = countByStatus.GetValueOrDefault(TodoStatus.Cancelada, 0),

                OverdueTasks = overdueCount,
                AverageDaysToComplete = averageDays
            };

            return Ok(stats); //200
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteTodoItem(int id)
        {
            var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var todoItem = await _context.TodoItems.FindAsync(id);
            if (todoItem == null) return NotFound();
            if (todoItem.OwnerId != ownerId) return NotFound();

            _context.TodoItems.Remove(todoItem);
            await _context.SaveChangesAsync();

            return NoContent(); //204
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> UpdateTodoItem(int id, TodoItem updated)
        {
            var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var todoItem = await _context.TodoItems.FindAsync(id);

            if (todoItem == null) return NotFound(); //404
            if (todoItem.OwnerId != ownerId) return NotFound();

            if (updated.CategoryId.HasValue)
            {
                var categoryExists = await _context.Categories.AnyAsync(t => t.Id == updated.CategoryId);
                if (!categoryExists) return BadRequest("The specified category does not exist");
            }

            todoItem.Title = updated.Title;
            todoItem.Description = updated.Description;
            todoItem.CategoryId = updated.CategoryId;
            todoItem.DueDate = ToUtc(updated.DueDate);


            await _context.SaveChangesAsync();

            return NoContent(); //204
        }
    }
}