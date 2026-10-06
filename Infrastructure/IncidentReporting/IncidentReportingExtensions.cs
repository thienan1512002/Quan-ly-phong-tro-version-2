using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace QuanLyPhongTro.Infrastructure.IncidentReporting;

public static class IncidentReportingExtensions
{
    public static IServiceCollection AddIncidentReporting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AgentPlatformOptions>()
            .Bind(configuration.GetSection(AgentPlatformOptions.SectionName))
            .Validate(options => !options.ReportingEnabled ||
                (Uri.TryCreate(options.ControlPlaneUrl, UriKind.Absolute, out var uri) &&
                 uri.Scheme is "http" or "https" && string.IsNullOrEmpty(uri.UserInfo) &&
                 string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment)),
                "AgentPlatform:ControlPlaneUrl must be an HTTP(S) URL without credentials, query or fragment.")
            .Validate(options => !options.ReportingEnabled ||
                (!string.IsNullOrWhiteSpace(options.ProjectCode) && options.ProjectCode.Length <= 64),
                "AgentPlatform:ProjectCode must contain 1 to 64 characters.")
            .Validate(options => !options.ReportingEnabled ||
                options.Environment is "Development" or "Staging" or "Production",
                "AgentPlatform:Environment must be Development, Staging or Production.")
            .Validate(options => options.RequestTimeoutSeconds is > 0 and <= 300,
                "AgentPlatform:RequestTimeoutSeconds must be between 1 and 300.")
            .Validate(options => options.QueueCapacity > 0 && options.ThrottleWindowSeconds > 0,
                "AgentPlatform:QueueCapacity and ThrottleWindowSeconds must be positive.")
            .ValidateOnStart();

        services.TryAddSingleton<TimeProvider>(TimeProvider.System);
        services.TryAddSingleton<IIncidentReportFilter, DefaultIncidentReportFilter>();
        services.AddSingleton<IncidentDataSanitizer>();
        services.AddSingleton<IncidentReportQueue>();
        services.AddSingleton<IIncidentReporter, IncidentReporter>();
        services.AddHostedService<IncidentReportWorker>();
        services.AddHttpClient(IncidentReporter.HttpClientName, (provider, client) =>
        {
            var settings = provider.GetRequiredService<IOptions<AgentPlatformOptions>>().Value;
            client.BaseAddress = new Uri(settings.ControlPlaneUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(settings.RequestTimeoutSeconds);
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AllowAutoRedirect = false,
            UseCookies = false
        });
        return services;
    }

    public static WebApplication MapDevelopmentIncidentTestEndpoint(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
            app.MapGet("/dev/test-agent-incident", ThrowTestIncident).AllowAnonymous();
        return app;
    }

    private static IResult ThrowTestIncident() =>
        throw new InvalidOperationException("NMV Agent Platform test incident");
}
