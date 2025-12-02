using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using System.Net.Http.Headers;
using System;

var builder = Host.CreateEmptyApplicationBuilder(settings: null);

// Configure logging: use a provider that writes to stderr so we don't pollute
// the MCP stdio transport which expects JSON-RPC messages on stdout.
builder.Logging.ClearProviders();
// Prefer a dedicated log file to avoid any stdout pollution of the MCP protocol.
var logFile = Environment.GetEnvironmentVariable("WEATHER_LOG_FILE") ?? "logs/weather.log";
builder.Logging.AddProvider(new FileLoggerProvider(logFile));
builder.Logging.SetMinimumLevel(LogLevel.Information);

builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

// Register an HttpClient that uses a DelegatingHandler which logs requests/responses
builder.Services.AddSingleton(sp =>
{
    var logger = sp.GetRequiredService<ILogger<QuickstartWeatherServer.Tools.HttpLoggingHandler>>();
    var handler = new QuickstartWeatherServer.Tools.HttpLoggingHandler(logger)
    {
        InnerHandler = new HttpClientHandler()
    };

    var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.weather.gov") };
    client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("weather-tool", "1.0"));
    return client;
});

var app = builder.Build();

// Initialize the logger in WeatherTools so it can be used by MCP tool wrappers
var weatherLoggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
var weatherLogger = weatherLoggerFactory.CreateLogger("WeatherTools");
QuickstartWeatherServer.Tools.WeatherTools.InitializeLog(weatherLogger);

// If requested, run a short local test to exercise HttpClient and logging.
if (Environment.GetEnvironmentVariable("WEATHER_LOCAL_TEST") == "1")
{
    // Build a service provider from the collection to resolve HttpClient and logging
    using var sp = app.Services.CreateScope();
    var client = sp.ServiceProvider.GetRequiredService<HttpClient>();
    var loggerFactory = sp.ServiceProvider.GetRequiredService<ILoggerFactory>();
    var logger = loggerFactory.CreateLogger("LocalTest");

    try
    {
        // Example coordinates (latitude, longitude) - you can change these
        double lat = 39.7456;
        double lon = -97.0892;

        logger.LogInformation("Running local GetForecast test for {Lat},{Lon}", lat, lon);
        var forecast = await QuickstartWeatherServer.Tools.WeatherTools.GetForecast(client, lat, lon, logger);
        Console.WriteLine("--- Forecast result (truncated) ---");
        Console.WriteLine(forecast);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine("Local test failed: " + ex);
    }

    return;
}

await app.RunAsync();