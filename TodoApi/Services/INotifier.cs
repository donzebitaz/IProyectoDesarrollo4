using TodoApi.Models;

namespace TodoApi.Services
{
    public interface INotifier
    {
        Task NotificarAsync(TodoItem todoItem);
    }
}
