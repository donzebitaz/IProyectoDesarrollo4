using TodoApi.Models;

namespace TodoApi.Models
{
    public class UpdateStatusDto
    {
        public TodoStatus Status { get; set; }
        public bool Force { get; set; } = false;
    }
}