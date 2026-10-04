using System.ComponentModel.DataAnnotations;

namespace TodoApi.Models
{
    public enum TodoStatus
    {
        Pending,
        InProgress,
        Completed,
        Canceled
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

        // The state machine is now used:
        // TodoStatus instead of the boolean IsCompleted
        public TodoStatus Status {get;set;} = TodoStatus.Pending;

        public DateTime CreatedAt {get;set;} = DateTime.UtcNow; 
        public DateTime? CompletedAt {get;set;} 
        public DateTime? DueDate {get;set;}

        // Stores the due date that was already notified to avoid duplicates
        public DateTime? LastNotifiedDueDate {get;set;}

        // User who created the task
        public string OwnerId {get;set;} = string.Empty;

        // Foreign key
        public int? CategoryId {get;set;}
        public Category? Category {get;set;}
    }
}