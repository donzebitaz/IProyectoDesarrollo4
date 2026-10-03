using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace TodoApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TodoItemsController : ControllerBase
    {
        private readonly TodoDbContext _context;
        public TodoItemsController(TodoDbContext context)
        {
            _context = context;
        }
        //SYNC es cuando tengo una app y la aplicación se queda bloqueada hasta que la BD responda
        //ASYNC es cuando la aplicación realiza una petición a la base de datos y libera el hilo de ejecución actual mientras espera la respuesta, permitiendo que la aplicación continúe procesando otras solicitudes sin bloquearse.

        //Diccionario de estados
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

        [HttpGet("{id:int}")]//indicando qué endpoint es
        public async Task<ActionResult<TodoItem>> GetTodoItem(int id)
        {
            var ownerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            
            var todoItem = await _context.TodoItems
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == id);//del contexto, saque que item es y guardelo
            //await -> cuando lo de la derecha funcione

            if (todoItem == null) return NotFound();//404
            if (todoItem.OwnerId != ownerId) return NotFound();

            return Ok(todoItem); //200
            //early returns, terminar un método antes de tiempo (no tiene else porque ya se valida arriba)
        }

        [HttpPost]
        public async Task<ActionResult<TodoItem>> CreateTodoItem(TodoItem todoItem)
        {
            //validar si categoryid trae algo
            //en caso positivo validar que el categoryid es válido
            //en caso de que no sea válido retornar un BadRequest()
            if (todoItem.CategoryId.HasValue)
            {
                var categoryExists = await _context.Categories.AnyAsync(t => t.Id == todoItem.CategoryId);
                if (!categoryExists) return BadRequest("The specified category does not exist");//early return valido lo que puede salir mal y al final lo que quiero que salga completamente
            }


            //fix pendiente: para que el cliente no decida el estado y toda tarea nueva esté en "Pendiente".
            todoItem.Status = TodoStatus.Pendiente;
            todoItem.CompletedAt = null;

            // El servidor es quien decide el dueño, sin importar que venga dentro del body
            todoItem.OwnerId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            // El servidor decide cuándo se creó 
            todoItem.CreatedAt = DateTime.UtcNow;

            todoItem.DueDate = ToUtc(todoItem.DueDate);

            //hey contexto, vaya su lista de TodoItems y agregue este elemento
            _context.TodoItems.Add(todoItem);

            //hey contexto, ahora sí guarde los cambios en la base de datos
            await _context.SaveChangesAsync();

            ///return ok, but not works, because of convection
            return CreatedAtAction(nameof(GetTodoItem), new { id = todoItem.Id }, todoItem);//201: created and info of the new object
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

            return Ok(todoItem);
        }

        [HttpGet] //IEnumerable, el más recomendado / Task<ActionResult<List<TodoItem>> tambien sirve
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

            // solo se muestran las tareas vencidas que todavía no están en un estado final.
            if (overdue == true)
            {
                var now = DateTime.UtcNow;
                query = query.Where(t => t.DueDate != null && t.DueDate < now && t.Status != TodoStatus.Completada
                    && t.Status != TodoStatus.Cancelada);
            }

            return Ok(await query.ToListAsync());//200
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

            return NoContent();
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

            return NoContent();
        }
    }
}
//HTTP GET
//HTTP POST
//HTTP PUT
//HTTP PATCH
//HTTP DELETE

//Lista de todos los TODO
//Muestre un TODO en particular
//Agregar nuevos TODO
//Modificar/actualizar TODO
//Eliminar TODO