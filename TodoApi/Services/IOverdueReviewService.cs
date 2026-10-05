// @author: Sebastián Alvarado García C5C341
// @author: Justin Andrés Badilla Ramírez C4C928
// @author: Abigail Crystal García Bonilla C5F263
// @author: Frank de Jesús Villalobos Elizondo C5K944

namespace TodoApi.Services
{
    // Contrato de la revision de tareas vencidas
    public interface IOverdueReviewService
    {
        Task<int> ReviewAndNotifyAsync(string? ownerId = null, CancellationToken cancellationToken = default);
    }
}