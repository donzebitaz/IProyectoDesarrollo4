using System.ComponentModel.DataAnnotations;

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
        // TodoStatus en lugar de IsCompleted tipo booleano
        public TodoStatus Status {get;set;} = TodoStatus.Pendiente;

        public DateTime CreatedAt {get;set;} = DateTime.UtcNow; 
        public DateTime? CompletedAt {get;set;} 
        public DateTime? DueDate {get;set;}

        // Usuario que creo la tarea
        public string OwnerId {get;set;} = string.Empty;

        //llave foránea
        public int? CategoryId {get;set;}
        public Category? Category {get;set;}
    }
}