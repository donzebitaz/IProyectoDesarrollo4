using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Models;

namespace TodoApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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

        [HttpGet("{id:int}")]//indicando qué endpoint es
        public async Task<ActionResult<TodoItem>> GetTodoItem(int id)
        {
            var todoItem = await _context.TodoItems
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == id);//del contexto, saque que item es y guardelo
            //await -> cuando lo de la derecha funcione

            if(todoItem == null) return NotFound();//404

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
            //hey contexto, vaya su lista de TodoItems y agregue este elemento
            _context.TodoItems.Add(todoItem);

            //hey contexto, ahora sí guarde los cambios en la base de datos
            await _context.SaveChangesAsync();

            ///return ok, but not works, because of convection
            return CreatedAtAction(nameof (GetTodoItem), new {id = todoItem.Id}, todoItem);//201: created and info of the new object
        }

        [HttpPatch("{id:int}/status")]
        public async Task<ActionResult<TodoItem>> UpdateStatus(int id, UpdateStatusDto dto)
        {
            var todoItem = await _context.TodoItems.FindAsync(id);
            if (todoItem == null) return NotFound();

            var currentStatus = todoItem.Status;

            if (!AllowedTransitions[currentStatus].Contains(dto.Status))
            {
                return BadRequest($"Cannot transition from {currentStatus} to {dto.Status}.");
            }

            todoItem.Status = dto.Status;
            if (dto.Status == TodoStatus.Completada)
            {
                todoItem.CompletedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            return Ok(todoItem);
        }

        [HttpGet] //IEnumerable, el más recomendado / Task<ActionResult<List<TodoItem>> tambien sirve
        public async Task<ActionResult<IEnumerable<TodoItem>>> GetTodoItems([FromQuery] TodoStatus? status, [FromQuery] int? categoryId)
        {
            var query = _context.TodoItems.Include(t => t.Category).AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(t => t.Status == status.Value);
            }

            if (categoryId.HasValue)
            {
                query = query.Where(t => t.CategoryId == categoryId.Value);
            }
                
            return Ok(await query.ToListAsync());//200
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteTodoItem(int id)
        {
            var todoItem = await _context.TodoItems.FindAsync(id);
            if (todoItem == null) return NotFound();

            _context.TodoItems.Remove(todoItem);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> UpdateTodoItem(int id, TodoItem updated)
        {
            var todoItem = await _context.TodoItems.FindAsync(id);

            if(todoItem == null) return NotFound(); //404

            if(updated.CategoryId.HasValue)
            {
                var categoryExists = await _context.Categories.AnyAsync(t => t.Id == updated.CategoryId);
                if(!categoryExists) return BadRequest("The specified category does not exist");
            }

            todoItem.Title = updated.Title;
            todoItem.Description = updated.Description;
            todoItem.CategoryId = updated.CategoryId;

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