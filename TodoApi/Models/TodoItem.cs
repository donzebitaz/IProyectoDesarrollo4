using System.ComponentModel.DataAnnotations;

// @author: Sebastián Alvarado García C5C341
// @author: Justin Andrés Badilla Ramírez C4C928
// @author: Abigail Crystal García Bonilla C5F263
// @author: Frank de Jesús Villalobos Elizondo C5K944

namespace TodoApi.Models
{
    public enum TodoStatus
    {
        Pendiente,
        EnProgreso,
        Completada,
        Cancelada
    }

    public class TodoItem 
    {
        [Key]    
        public int Id {get;set;} 
        
        [Required]
        [MaxLength(200)] 
        public string Title {get;set;} = string.Empty; 
        
        [MaxLength(1000)]
        public string Description {get;set;} = string.Empty;

        // Ahora se utiliza la maquina de estados
        // TodoStatus en lugar del booleano IsCompleted
        public TodoStatus Status {get;set;} = TodoStatus.Pendiente;

        public DateTime CreatedAt {get;set;} = DateTime.UtcNow; 
        public DateTime? CompletedAt {get;set;} 
        public DateTime? DueDate {get;set;}

        // Guarda la fecha de vencimiento que ya fue notificada para evitar duplicados
        public DateTime? LastNotifiedDueDate {get;set;}

        // Usuario que creo la tarea
        public string OwnerId {get;set;} = string.Empty;

        // Llave foranea
        public int? CategoryId {get;set;}
        public Category? Category {get;set;}
    }
}