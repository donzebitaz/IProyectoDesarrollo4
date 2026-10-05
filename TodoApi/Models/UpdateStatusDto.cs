using TodoApi.Models;

// @author: Sebastián Alvarado García C5C341
// @author: Justin Andrés Badilla Ramírez C4C928
// @author: Abigail Crystal García Bonilla C5F263
// @author: Frank de Jesús Villalobos Elizondo C5K944

namespace TodoApi.Models
{
    public class UpdateStatusDto
    {
        public TodoStatus Status { get; set; }
        public bool Force { get; set; } = false;
    }
}