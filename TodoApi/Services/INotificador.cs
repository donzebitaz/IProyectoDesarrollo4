using TodoApi.Models;

namespace TodoApi.Services
{
    public interface INotificador
    {
        Task NotificarAsync(TodoItem todoItem);
    }
}
