using System.Threading.Channels;
using System.Runtime.CompilerServices;
using Microsoft.Data.Sqlite;
using FreightningScrapper;
;

var builder = WebApplication.CreateBuilder(args);

// Configurar servicios
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSignalR();

builder.Services.AddSingleton(_ => new SqliteConnection("Data Source=tracking.db"));
builder.Services.AddSingleton<ITrackingRepository, TrackingRepository>();
builder.Services.AddSingleton<OrderTrackerHub>();
builder.Services.AddHostedService<OrderTrackerBackgroundService>();



var app = builder.Build();

app.UseHttpsRedirection();

// Serve the index.html file as the default entry page
app.UseDefaultFiles();
app.UseStaticFiles();

// Configurar SignalR hub
app.MapHub<OrderTrackerHub>("/ordertracker");

// Initialize the database
await app.Services.GetRequiredService<ITrackingRepository>().InitializeDatabaseAsync();

app.Run();
