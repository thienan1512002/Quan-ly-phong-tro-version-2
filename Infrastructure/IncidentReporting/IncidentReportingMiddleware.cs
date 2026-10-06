using Microsoft.Extensions.Options;

namespace QuanLyPhongTro.Infrastructure.IncidentReporting;

public sealed class IncidentReportingMiddleware(
    RequestDelegate next,
    IncidentReportQueue queue,
    IIncidentReportFilter filter,
    IncidentDataSanitizer sanitizer,
    IOptions<AgentPlatformOptions> options,
    ILogger<IncidentReportingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            try
            {
                if (options.Value.ReportingEnabled && filter.ShouldReport(exception, context))
                {
                    var incident = Capture(exception, context);
                    if (!queue.TryEnqueue(incident))
                        logger.LogWarning("Incident queue is full; incident dropped, correlation {CorrelationId}", incident.CorrelationId);
                }
            }
            catch (Exception reportingException)
            {
                logger.LogWarning("Incident capture failed with {ErrorType}; the original application exception will be rethrown",
                    reportingException.GetType().Name);
            }

            // Keep the original stack and let the existing exception handler build the response.
            throw;
        }
    }

    private IncidentPayload Capture(Exception exception, HttpContext context)
    {
        var settings = options.Value;
        var endpoint = sanitizer.Sanitize(context.Request.Path.Value ?? "/", 2048, context) ?? "/";
        return new IncidentPayload
        {
            ProjectCode = settings.ProjectCode,
            Environment = settings.Environment,
            Title = sanitizer.Sanitize($"Unhandled {exception.GetType().Name} at {endpoint}", 256, context) ?? "Unhandled application exception",
            ErrorType = sanitizer.Sanitize(exception.GetType().FullName ?? exception.GetType().Name, 512, context) ?? "Exception",
            ErrorMessage = sanitizer.Sanitize(exception.Message, 4096, context) ?? string.Empty,
            StackTrace = sanitizer.Sanitize(exception.StackTrace, 32768, context),
            Endpoint = endpoint,
            CorrelationId = sanitizer.Sanitize(context.TraceIdentifier, 256, context) ?? string.Empty,
            DeploymentVersion = sanitizer.Sanitize(settings.DeploymentVersion, 256, context),
            GitCommit = sanitizer.Sanitize(settings.GitCommit, 128, context)
        };
    }
}
