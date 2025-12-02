using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace QuickstartWeatherServer.Tools;

public class HttpLoggingHandler : DelegatingHandler
{
    private readonly ILogger<HttpLoggingHandler> _logger;

    public HttpLoggingHandler(ILogger<HttpLoggingHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("HTTP Request: {Method} {Uri}", request.Method, request.RequestUri);

        try
        {
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            sw.Stop();
            _logger.LogInformation("HTTP Response: {StatusCode} for {Method} {Uri} in {Elapsed}ms", (int)response.StatusCode, request.Method, request.RequestUri, sw.Elapsed.TotalMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "HTTP Request failed: {Method} {Uri} after {Elapsed}ms", request.Method, request.RequestUri, sw.Elapsed.TotalMilliseconds);
            throw;
        }
    }
}
