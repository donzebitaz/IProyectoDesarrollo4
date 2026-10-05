namespace TodoApi.Services
{
    // Contrato de la revision de tareas vencidas
    public interface IOverdueReviewService
    {
        Task<int> ReviewAndNotifyAsync(string? ownerId = null, CancellationToken cancellationToken = default);
    }
}