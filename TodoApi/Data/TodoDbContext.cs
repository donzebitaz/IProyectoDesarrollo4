using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TodoApi.Models;

// @author: Sebastián Alvarado García C5C341
// @author: Justin Andrés Badilla Ramírez C4C928
// @author: Abigail Crystal García Bonilla C5F263
// @author: Frank de Jesús Villalobos Elizondo C5K944

namespace TodoApi.Data;
//contexto de la base de datos
public class TodoDbContext : IdentityDbContext<IdentityUser>
{
    public TodoDbContext(DbContextOptions<TodoDbContext> options) :base(options)
    {}
    public DbSet<TodoItem> TodoItems {get;set;} 
    public DbSet<Category> Categories { get; set; }
}