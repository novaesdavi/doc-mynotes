using ModelContextProtocol.Server;
using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace QuickstartWeatherServer.Tools;

[McpServerToolType]
public static class WeatherTools
{
    private static ILogger? _logger;

    /// <summary>
    /// Initialize the logger for WeatherTools. Call this from Program.cs after building the service provider.
    /// </summary>
    public static void InitializeLog(ILogger? logger)
    {
        _logger = logger;
    }
    public static async Task<string> GetAlerts(
        HttpClient client,
        [Description("The US state to get alerts for.")] string state,
        ILogger? logger = null)
    {
        logger?.LogInformation("GetAlerts called for state {State}", state);
        using var jsonDocument = await client.ReadJsonDocumentAsync($"/alerts/active/area/{state}");
        var jsonElement = jsonDocument.RootElement;
        var alerts = jsonElement.GetProperty("features").EnumerateArray();

        if (!alerts.Any())
        {
            logger?.LogInformation("No active alerts for state {State}", state);
            return "No active alerts for this state.";
        }

        try
        {
            var result = string.Join("\n--\n", alerts.Select(alert =>
            {
                JsonElement properties = alert.GetProperty("properties");
                return $"""
                        Event: {properties.GetProperty("event").GetString()}
                        Area: {properties.GetProperty("areaDesc").GetString()}
                        Severity: {properties.GetProperty("severity").GetString()}
                        Description: {properties.GetProperty("description").GetString()}
                        Instruction: {properties.GetProperty("instruction").GetString()}
                        """;
            }));

            logger?.LogInformation("GetAlerts returning {Count} alerts for state {State}", alerts.Count(), state);
            return result;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "GetAlerts failed for state {State}", state);
            throw;
        }
    }

    // Public wrapper exposed as the MCP tool (no ILogger parameter)
    [McpServerTool, Description("Get weather alerts for a US state.")]
    public static Task<string> GetAlerts(
        HttpClient client,
        [Description("The US state to get alerts for.")] string state)
    {
        return GetAlerts(client, state, _logger);
    }

    public static async Task<string> GetForecast(
        HttpClient client,
        [Description("Latitude of the location.")] double latitude,
        [Description("Longitude of the location.")] double longitude,
        ILogger? logger = null)
    {
        logger?.LogInformation("GetForecast called for {Lat},{Lon}", latitude, longitude);

        var pointUrl = string.Create(CultureInfo.InvariantCulture, $"/points/{latitude},{longitude}");
        using var jsonDocument = await client.ReadJsonDocumentAsync(pointUrl);
        var forecastUrl = jsonDocument.RootElement.GetProperty("properties").GetProperty("forecast").GetString()
            ?? throw new Exception($"No forecast URL provided by {client.BaseAddress}points/{latitude},{longitude}");

        try
        {
            using var forecastDocument = await client.ReadJsonDocumentAsync(forecastUrl);
            var periods = forecastDocument.RootElement.GetProperty("properties").GetProperty("periods").EnumerateArray();

            var result = string.Join("\n---\n", periods.Select(period => $"""
                    {period.GetProperty("name").GetString()}
                    Temperature: {period.GetProperty("temperature").GetInt32()}°F
                    Wind: {period.GetProperty("windSpeed").GetString()} {period.GetProperty("windDirection").GetString()}
                    Forecast: {period.GetProperty("detailedForecast").GetString()}
                    """));

            logger?.LogInformation("GetForecast returning {Periods} periods for {Lat},{Lon}", periods.Count(), latitude, longitude);
            return result;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "GetForecast failed for {Lat},{Lon}", latitude, longitude);
            throw;
        }
    }

    // Public wrapper exposed as the MCP tool (no ILogger parameter)
    [McpServerTool, Description("Get weather forecast for a location.")]
    public static Task<string> GetForecast(
        HttpClient client,
        [Description("Latitude of the location.")] double latitude,
        [Description("Longitude of the location.")] double longitude)
    {
        return GetForecast(client, latitude, longitude, _logger);
    }
}

public static class HttpClientJsonExtensions
{
    public static async Task<JsonDocument> ReadJsonDocumentAsync(this HttpClient client, string requestUri)
    {
        if (client is null) throw new ArgumentNullException(nameof(client));
        if (string.IsNullOrWhiteSpace(requestUri)) throw new ArgumentNullException(nameof(requestUri));

        // Resolve absolute or relative URI against client's BaseAddress
        Uri uri;
        if (Uri.TryCreate(requestUri, UriKind.Absolute, out Uri? maybeUri))
        {
            uri = maybeUri;
        }
        else
        {
            if (client.BaseAddress == null)
                throw new InvalidOperationException("HttpClient.BaseAddress is null and a relative URI was provided.");
            uri = new Uri(client.BaseAddress, requestUri);
        }

        using var resp = await client.GetAsync(uri).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();

        await using var stream = await resp.Content.ReadAsStreamAsync().ConfigureAwait(false);
        var doc = await JsonDocument.ParseAsync(stream).ConfigureAwait(false);
        return doc;
    }
}