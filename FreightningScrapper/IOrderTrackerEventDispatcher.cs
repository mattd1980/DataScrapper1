
namespace FreightningScrapper;

public interface IOrderTrackerEventDispatcher
{
    Task DispatchUpdateAsync(List<StatusHistory> updates);
    Task AddClientAsync(string connectionId, string name, string[] trackingNumbers);
    Task RemoveClientAsync(string connectionId);
}
