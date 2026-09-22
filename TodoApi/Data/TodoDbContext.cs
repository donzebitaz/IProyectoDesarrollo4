using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TodoApi.Models;

namespace TodoApi.Data;
//contexto de la base de datos
public class TodoDbContext : IdentityDbContext<IdentityUser>
{
    public TodoDbContext(DbContextOptions<TodoDbContext> options) :base(options)
    {}
    //aquí se le dice con qué va a trabajar 
    public DbSet<TodoItem> TodoItems {get;set;} // se agrega una lista de las tablas
    public DbSet<Category> Categories { get; set; }
}