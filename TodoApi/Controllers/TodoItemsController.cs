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

        // Dictionary of allowed status transitions
        private static readonly Dictionary<TodoStatus, TodoStatus[]> AllowedTransitions = new()
        {
            [TodoStatus.Pending] = new[] { TodoStatus.InProgress, TodoStatus.Canceled },
            [TodoStatus.InProgress] = new[] { TodoStatus.Completed, TodoStatus.Canceled },
            [TodoStatus.Completed] = Array.Empty<TodoStatus>(),
            [TodoStatus.Canceled] = Array.Empty<TodoStatus>()
        };


        // Helper used to store dates in UTC
        private static DateTime? ToUtc(DateTime? date)
        {
            if (!date.HasValue) return null;

            return date.Value.Kind switch
            {
                DateTimeKind.Utc => date.Value, // Already in UTC
                DateTimeKind.Local => date.Value.ToUniversalTime(), // Convert to UTC
                _ => DateTime.SpecifyKind(date.Value, DateTimeKind.Utc) // No time zone specified, assume UTC
            };
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<TodoItem>> GetTodoItem(int id)
        {
            var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var todoItem = await _context.TodoItems
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == id);

            if (todoItem == null) return NotFound(); // Not Found (404)
            if (todoItem.OwnerId != ownerId) return NotFound();

            return Ok(todoItem); // OK (200)
            // Early returns: end a method before reaching its last line
        }

        [HttpPost]
        public async Task<ActionResult<TodoItem>> CreateTodoItem(TodoItem todoItem)
        {
            // Check whether CategoryId has a value
            // If so, check that the CategoryId is valid
            // If it is not valid, return a BadRequest()
            if (todoItem.CategoryId.HasValue)
            {
                var categoryExists = await _context.Categories.AnyAsync(t => t.Id == todoItem.CategoryId);
                if (!categoryExists) return BadRequest("The specified category does not exist"); // Early return: check what can go wrong first and leave the desired outcome for the end
            }


            // Fix: the client must not decide the status, every new task starts as Pending
            todoItem.Status = TodoStatus.Pending;
            todoItem.CompletedAt = null;

            // The server decides the owner, regardless of what comes in the body
            todoItem.OwnerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            // The server decides when the task was created
            todoItem.CreatedAt = DateTime.UtcNow;

            todoItem.DueDate = ToUtc(todoItem.DueDate);

            _context.TodoItems.Add(todoItem);

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTodoItem), new { id = todoItem.Id }, todoItem); // Created (201) with the info of the new object
        }

        [HttpPost("notificarvencidas")]
        public async Task<ActionResult> NotifyOverdue()
        {
            var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            // Only the overdue tasks of the authenticated user
            var notified = await _overdueReviewService.ReviewAndNotifyAsync(ownerId);

            return Ok(new { count = notified }); // OK 200
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

            if (dto.Status == TodoStatus.Completed && todoItem.DueDate.HasValue && todoItem.DueDate.Value < DateTime.UtcNow
               && !dto.Force)
            {
                return BadRequest("The task is overdue. Set \"force\": true to complete it anyway.");
            }

            todoItem.Status = dto.Status;
            if (dto.Status == TodoStatus.Completed)
            {
                todoItem.CompletedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Ok(todoItem); // OK 200
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

            // Only overdue tasks that have not reached a final status are shown
            if (overdue == true)
            {
                var now = DateTime.UtcNow;
                query = query.Where(t => t.DueDate != null && t.DueDate < now && t.Status != TodoStatus.Completed
                    && t.Status != TodoStatus.Canceled);
            }

            return Ok(await query.ToListAsync()); // OK 200
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

            return NoContent(); // No Content 204
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> UpdateTodoItem(int id, TodoItem updated)
        {
            var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var todoItem = await _context.TodoItems.FindAsync(id);

            if (todoItem == null) return NotFound(); // Not Found (404)
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

            return NoContent(); // No Content 204
        }
    }
}