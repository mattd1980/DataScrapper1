using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        AppLogger.Info("Starting Order Tracker App...");

        Console.Write("Enter Tracking Number: ");
        var trackingNumber = Console.ReadLine();

        var tracker = new OrderTracker();
        try
        {
            var history = await tracker.GetStatusHistoryAsync(trackingNumber);

            if (history.Count == 0)
            {
                AppLogger.Warn("No status history found for that tracking number.");
            }
            else
            {
                AppLogger.Success($"Found {history.Count} status entries.");
                Console.WriteLine("\n=== Status History ===");
                foreach (var entry in history)
                {
                    Console.WriteLine($"{entry.Timestamp} | {entry.StatusCode}");
                }
            }
        }
        catch (Exception ex)
        {
            AppLogger.Error($"An error occurred: {ex.Message}");
        }

        AppLogger.Info("Done.");
    }
}
