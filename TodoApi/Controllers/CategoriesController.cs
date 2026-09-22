using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Models;

namespace TodoApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CategoriesController : ControllerBase
    {
        private readonly TodoDbContext _context;

        public CategoriesController(TodoDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Category>>> GetCategories()
        {
            var categories = await _context.Categories.ToListAsync();
            return Ok(categories);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Category>> GetCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);

            if (category == null) return NotFound();

            return Ok(category);
        }

        [HttpPost]
        public async Task<ActionResult<Category>> CreateCategory(Category category)
        {
            _context.Categories.Add(category);

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateCategory(int id, Category category)
        {
            if (id != category.Id) return BadRequest();

            var exists = await _context.Categories.AnyAsync(c => c.Id == id);
            if (!exists) return NotFound();

            _context.Entry(category).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Eliminar una categoria
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);

            if (category == null) return NotFound(); //404

            //Verificar si la categoria tiene todoItemas asignados
            var hasTodoItems = await _context.TodoItems
                .AnyAsync(t => t.CategoryId == id);


            // En caso positivo retornar un Conflict()
            if (hasTodoItems)
            {
                return Conflict(
                    "The category cannot be deleted because it has TodoItems assigned to it."
                ); //409
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent(); //204
        }
    }
}