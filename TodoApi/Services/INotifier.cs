using TodoApi.Models;

// @author: Sebastián Alvarado García C5C341
// @author: Justin Andrés Badilla Ramírez C4C928
// @author: Abigail Crystal García Bonilla C5F263
// @author: Frank de Jesús Villalobos Elizondo C5K944

namespace TodoApi.Services
{
    public interface INotifier
    {
        Task NotifyAsync(TodoItem todoItem);
    }
}
