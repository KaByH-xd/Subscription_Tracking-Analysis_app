using SubscriptionTracker.Models;

namespace SubscriptionTracker.Services;

public interface IDatabaseService
{
    Task<List<Subscription>> GetAllAsync();
    Task SaveAsync(Subscription subscription);
    Task DeleteAsync(Subscription subscription);
    Task DeleteAllAsync();
    Task AddDemoDataAsync();
}
