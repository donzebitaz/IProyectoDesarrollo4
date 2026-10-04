namespace TodoApi.Services
{
    // Contrato de la revisión de tareas vencidas.
    public interface IRevisionVencidasService
    {
        Task<int> RevisarYNotificarAsync(string? ownerId = null, CancellationToken cancellationToken = default);
    }
}