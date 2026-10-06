using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace QuanLyPhongTro.Infrastructure.IncidentReporting;

public sealed class IncidentReportWorker(
    IncidentReportQueue queue,
    IIncidentReporter reporter,
    IOptions<AgentPlatformOptions> options,
    TimeProvider clock,
    ILogger<IncidentReportWorker> logger) : BackgroundService
{
    private readonly Dictionary<string, long> _lastReported = new(StringComparer.Ordinal);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.ReportingEnabled)
        {
            if (options.Value.Enabled && options.Value.AutoReportIncidents &&
                string.IsNullOrWhiteSpace(options.Value.ControlPlaneUrl))
                logger.LogWarning("AgentPlatform:ControlPlaneUrl is empty; incident reporting is inactive until a URL is configured and the application restarts.");
            return;
        }

        try
        {
            await foreach (var incident in queue.ReadAllAsync(stoppingToken))
            {
                var timestamp = clock.GetTimestamp();
                var window = TimeSpan.FromSeconds(options.Value.ThrottleWindowSeconds);
                // Expire entries so unique failures don't accumulate for the lifetime of the app.
                foreach (var key in _lastReported.Where(entry =>
                    clock.GetElapsedTime(entry.Value, timestamp) >= window).Select(entry => entry.Key).ToArray())
                    _lastReported.Remove(key);

                var fingerprint = GetFingerprint(incident);
                if (_lastReported.ContainsKey(fingerprint))
                {
                    logger.LogDebug("Incident suppressed by source throttle, correlation {CorrelationId}", incident.CorrelationId);
                    continue;
                }

                try
                {
                    if (await reporter.ReportAsync(incident, stoppingToken))
                        _lastReported[fingerprint] = clock.GetTimestamp();
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    // Reporting infrastructure must never stop the application host.
                    logger.LogWarning("Incident worker failed with {ErrorType}, correlation {CorrelationId}",
                        exception.GetType().Name, incident.CorrelationId);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal BackgroundService shutdown.
        }
    }

    private static string GetFingerprint(IncidentPayload incident)
    {
        var frames = (incident.StackTrace ?? string.Empty)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var firstRelevantFrame = frames.FirstOrDefault(frame =>
            !frame.StartsWith("at System.", StringComparison.Ordinal) &&
            !frame.StartsWith("at Microsoft.", StringComparison.Ordinal)) ?? frames.FirstOrDefault() ?? string.Empty;
        var key = $"{incident.ErrorType}\n{incident.Endpoint}\n{firstRelevantFrame}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)));
    }
}
