namespace TodoApi.Services
{
    // Contract of the overdue task review
    public interface IOverdueReviewService
    {
        Task<int> ReviewAndNotifyAsync(string? ownerId = null, CancellationToken cancellationToken = default);
    }
}