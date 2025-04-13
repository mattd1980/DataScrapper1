using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;

namespace FreightningScrapper;


public interface ITrackingRepository
{
    Task InitializeDatabaseAsync();
    Task AddClientAsync(string clientId, string name);
    Task AddTrackingNumberAsync(string clientId, string trackingNumber);
    Task<Dictionary<(string clientId, string name), List<string>>> GetTrackingNumbersAsync(CancellationToken stoppingToken);
    Task RemoveClientAsync(string name);
    Task<string?> GetClientNameAsync(string clientId);
    Task<List<string>> GetTrackingNumbersByClientNameAsync(string clientName);
}

public class TrackingRepository(SqliteConnection connection) : ITrackingRepository
{
    public async Task InitializeDatabaseAsync()
    {
        try 
        {            
            await connection.OpenAsync();
            await connection.ExecuteAsync(@"
                CREATE TABLE IF NOT EXISTS Clients (
                    ClientId TEXT NOT NULL,
                    Name TEXT PRIMARY KEY NOT NULL
                );
            
                CREATE TABLE IF NOT EXISTS TrackingNumbers (
                    ClientName TEXT NOT NULL,
                    TrackingNumber TEXT NOT NULL,
                    PRIMARY KEY (ClientName, TrackingNumber),
                    FOREIGN KEY (ClientName) REFERENCES Clients(Name)
                );
            ");

            AppLogger.Info("Created database tables...");
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Error opening database connection. {ex}");
            throw;
        }
        finally
        {
            if(connection.State == ConnectionState.Open)
                await connection.CloseAsync();
        }
    }

    public async Task AddClientAsync(string clientId, string name)
    {
        try
        {
            await connection.OpenAsync();

            await connection.ExecuteAsync(
                @"INSERT INTO Clients (ClientId, Name) 
                VALUES (@ClientId, @Name)
                ON CONFLICT(Name) DO UPDATE SET ClientId = @ClientId;", 
                new { ClientId = clientId, Name = name });
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Error opening database connection. {ex}");
            throw;
        }
        finally
        {
            if(connection.State == ConnectionState.Open)
                await connection.CloseAsync();
        }
    }

    public async Task AddTrackingNumberAsync(string clientId, string trackingNumber)
    {
        try
        {
            await connection.OpenAsync();
            
            // First get the client name from the client ID
            var clientName = await connection.QuerySingleOrDefaultAsync<string>(
                "SELECT Name FROM Clients WHERE ClientId = @ClientId",
                new { ClientId = clientId });
                
            if (clientName != null)
            {
                await connection.ExecuteAsync(
                    @"INSERT INTO TrackingNumbers (ClientName, TrackingNumber) 
                    VALUES (@ClientName, @TrackingNumber)
                    ON CONFLICT(ClientName, TrackingNumber) DO UPDATE SET 
                    ClientName = @ClientName;", 
                    new { ClientName = clientName, TrackingNumber = trackingNumber });
            }
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Error opening database connection. {ex}");
            throw;
        }
        finally
        {
            if(connection.State == ConnectionState.Open)
                await connection.CloseAsync();
        }
    }

    public async Task<Dictionary<(string clientId, string name), List<string>>> GetTrackingNumbersAsync(CancellationToken cancelToken)
    {
        try
        {
            await connection.OpenAsync(cancelToken);

            Dictionary<(string clientId, string clientName), List<string>> store = [];

            await connection.QueryAsync<string, string, string, object?>(
                @"SELECT c.ClientId, t.ClientName, t.TrackingNumber 
                  FROM TrackingNumbers t
                  JOIN Clients c ON c.Name = t.ClientName",
                  (clientId, clientName, trackingNumber) => 
                  {
                    if(store.TryGetValue((clientId, clientName), out var list))
                        list.Add(trackingNumber);
                    else
                        store[(clientId, clientName)] = [trackingNumber];
                    
                    return default;
                  },
                  splitOn: "ClientName,TrackingNumber");

            return store;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Error opening database connection. {ex}");
            throw;
        }
        finally
        {
            if(connection.State == ConnectionState.Open)
                await connection.CloseAsync();
        }
    }

    public async Task RemoveClientAsync(string name)
    {
        try
        {
            await connection.OpenAsync();
            
            // Delete tracking numbers first, then delete the client (in a single query)
            await connection.ExecuteAsync(@"
                DELETE FROM TrackingNumbers WHERE ClientName = @name;
                DELETE FROM Clients WHERE Name = @name;
            ", new { name });
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Error opening database connection. {ex}");
            throw;
        }
        finally
        {
            if (connection.State == ConnectionState.Open)
                await connection.CloseAsync();
        }
    }

    public async Task<string?> GetClientNameAsync(string clientId)
    {
        try
        {
            await connection.OpenAsync();
            
            return await connection.QuerySingleOrDefaultAsync<string>(
                "SELECT Name FROM Clients WHERE ClientId = @ClientId",
                new { ClientId = clientId });
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Error getting client name: {ex.Message}");
            return string.Empty;
        }
        finally
        {
            if(connection.State == ConnectionState.Open)
                await connection.CloseAsync();
        }
    }

    public async Task<List<string>> GetTrackingNumbersByClientNameAsync(string clientName)
    {
        try
        {
            await connection.OpenAsync();


            return [.. await connection.QueryAsync<string>(
                "SELECT TrackingNumber FROM TrackingNumbers WHERE ClientName = @ClientName",
                new { ClientName = clientName })];
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Error getting tracking numbers by client name: {ex.Message}");
            return [];
        }
        finally
        {
            if(connection.State == ConnectionState.Open)
                await connection.CloseAsync();
        }
    }
}

public record TrackingTuple(string ClientName, string TrackingNumber);
public record TrackingInfo(string ClientName, string[] TrackingNumbers);