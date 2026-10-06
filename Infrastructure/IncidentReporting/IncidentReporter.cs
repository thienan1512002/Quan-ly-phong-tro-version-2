using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace QuanLyPhongTro.Infrastructure.IncidentReporting;

public sealed class IncidentReporter(
    IHttpClientFactory httpClientFactory,
    IOptions<AgentPlatformOptions> options,
    TimeProvider clock,
    ILogger<IncidentReporter> logger) : IIncidentReporter
{
    public const string HttpClientName = "AgentPlatformIncidents";
    private const int MaxAttempts = 3;

    public async Task<bool> ReportAsync(IncidentPayload incident, CancellationToken cancellationToken)
    {
        if (!options.Value.ReportingEnabled)
            return false;

        using var client = httpClientFactory.CreateClient(HttpClientName);
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                using var response = await client.PostAsJsonAsync("api/incidents", incident, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    logger.LogInformation("Incident submitted for {ProjectCode}, correlation {CorrelationId}",
                        incident.ProjectCode, incident.CorrelationId);
                    return true;
                }

                logger.LogWarning("Incident submission returned HTTP {StatusCode}, attempt {Attempt}/{MaxAttempts}, correlation {CorrelationId}",
                    (int)response.StatusCode, attempt, MaxAttempts, incident.CorrelationId);

                // Validation/authentication failures need configuration changes, not retries.
                if ((int)response.StatusCode < 500 && response.StatusCode is not
                    (HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests))
                    return false;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return false;
            }
            catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException)
            {
                // Do not log exception messages, response bodies or payloads: they can contain secrets.
                logger.LogWarning("Incident submission failed with {ErrorType}, attempt {Attempt}/{MaxAttempts}, correlation {CorrelationId}",
                    exception.GetType().Name, attempt, MaxAttempts, incident.CorrelationId);
            }

            if (attempt < MaxAttempts)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(1 << (attempt - 1)), clock, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    return false;
                }
            }
        }

        logger.LogWarning("Incident submission abandoned after {MaxAttempts} attempts, correlation {CorrelationId}",
            MaxAttempts, incident.CorrelationId);
        return false;
    }
}
