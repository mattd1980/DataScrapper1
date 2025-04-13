using Microsoft.AspNetCore.SignalR;

namespace FreightningScrapper;

public class OrderTrackerHub(ITrackingRepository trackingRepository) : Hub
{
    public async Task RegisterTrackingNumberAsync(string clientName, string trackingNumber)
    {
        if (string.IsNullOrWhiteSpace(clientName) || string.IsNullOrWhiteSpace(trackingNumber))
        {
            throw new ArgumentException("Client name and tracking number cannot be null or empty.");
        }

        await trackingRepository.AddClientAsync(Context.ConnectionId, clientName);
        await trackingRepository.AddTrackingNumberAsync(Context.ConnectionId, trackingNumber);

        AppLogger.Info($"Client {clientName} ({Context.ConnectionId}) is watching tracking number: {trackingNumber}");
    }

    public async Task GetStatusHistoryAsync(string clientName, string trackingNumber, CancellationToken cancelToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(clientName) || string.IsNullOrWhiteSpace(trackingNumber))
            {
                throw new ArgumentException("Client name and tracking number cannot be null or empty.");
            }
            // Get client name from the connection ID
            string connectionId = Context.ConnectionId;
            
            if (string.IsNullOrEmpty(clientName))
            {
                AppLogger.Warn($"Client with connection ID {connectionId} not found in repository");
                return;
            }

            // Get tracking numbers for the client
            var clientTracking = await trackingRepository.GetTrackingNumbersAsync(cancelToken);
            var clientKey = clientTracking.Keys.FirstOrDefault(k => k.clientId == connectionId);
            
            if (clientKey == default || !clientTracking.TryGetValue(clientKey, out var trackingNumbers) || trackingNumbers.Count == 0)
            {
                AppLogger.Warn($"No tracking numbers found for client {clientName} ({connectionId})");
                return;
            }

            // Use the same method as in background service to get status history
            AppLogger.Info($"Client {clientName} ({connectionId}) requested status history for their tracking numbers");
            
            await this.GetStatusHistoryAsync(connectionId, clientName, trackingNumber, cancelToken);    
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Error retrieving status history: {ex.Message}");
        }
    }
}
