using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using QuanLyPhongTro.Controllers;
using QuanLyPhongTro.Infrastructure.IncidentReporting;

namespace QuanLyPhongTro.Tests.IncidentReporting;

public sealed class IncidentReportingIntegrationTests
{
    [Fact]
    public async Task Development_test_endpoint_creates_incident_via_queue_worker_and_HTTP_reporter()
    {
        using var handler = new RecordingHandler();
        await using var app = await StartAppAsync(Environments.Development, handler);
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/dev/test-agent-incident");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var posted = await handler.NextAsync();
        Assert.Equal("https://control-plane.test/api/incidents", posted.Uri!.AbsoluteUri);
        using var json = JsonDocument.Parse(posted.Json);
        Assert.Equal("NMV Agent Platform test incident", json.RootElement.GetProperty("errorMessage").GetString());
        Assert.Equal("System.InvalidOperationException", json.RootElement.GetProperty("errorType").GetString());
        Assert.Equal("/dev/test-agent-incident", json.RootElement.GetProperty("endpoint").GetString());
        Assert.False(string.IsNullOrEmpty(json.RootElement.GetProperty("correlationId").GetString()));
    }

    [Fact]
    public async Task Production_reuses_existing_error_view_and_does_not_wait_for_the_control_plane()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var handler = new RecordingHandler
        {
            OnSend = async (_, token) =>
            {
                await release.Task.WaitAsync(token);
                return new HttpResponseMessage(HttpStatusCode.Created);
            }
        };
        await using var app = await StartAppAsync(Environments.Production, handler);
        using var client = app.GetTestClient();
        try
        {
            using var response = await client.GetAsync("/boom").WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("An error occurred while processing your request.", html);
            Assert.Contains("Request ID:", html);
            var posted = await handler.NextAsync();
            using var json = JsonDocument.Parse(posted.Json);
            Assert.Equal("/boom", json.RootElement.GetProperty("endpoint").GetString());
            Assert.False(release.Task.IsCompleted);
        }
        finally { release.TrySetResult(); }
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public async Task Test_endpoint_does_not_exist_outside_development(string environment)
    {
        using var handler = new RecordingHandler();
        await using var app = await StartAppAsync(environment, handler);
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/dev/test-agent-incident");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, handler.AttemptCount);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task Disabled_switch_does_not_send_HTTP_and_preserves_production_error_response(bool enabled, bool autoReport)
    {
        using var handler = new RecordingHandler();
        await using var app = await StartAppAsync(Environments.Production, handler, enabled, autoReport);
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/boom");
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("An error occurred while processing your request.", await response.Content.ReadAsStringAsync());
        Assert.Equal(0, handler.AttemptCount);
    }

    [Fact]
    public async Task Empty_control_plane_url_keeps_application_running_without_attempting_HTTP()
    {
        using var handler = new RecordingHandler();
        await using var app = await StartAppAsync(Environments.Production, handler, controlPlaneUrl: "");
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/boom");
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("An error occurred while processing your request.", await response.Content.ReadAsStringAsync());
        Assert.Equal(0, handler.AttemptCount);
    }

    [Fact]
    public async Task Named_HTTP_client_uses_configured_timeout_and_base_address()
    {
        var config = Configuration();
        var services = new ServiceCollection().AddLogging();
        services.AddSingleton<IConfiguration>(config);
        services.AddIncidentReporting(config);
        using var provider = services.BuildServiceProvider();
        using var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(IncidentReporter.HttpClientName);
        Assert.Equal(TimeSpan.FromSeconds(10), client.Timeout);
        Assert.Equal("https://control-plane.test/", client.BaseAddress!.AbsoluteUri);
    }

    [Fact]
    public void Invalid_control_plane_configuration_is_rejected_when_enabled()
    {
        var config = Configuration();
        config["AgentPlatform:ControlPlaneUrl"] = "https://user:password@control-plane.test";
        var services = new ServiceCollection().AddLogging();
        services.AddSingleton<IConfiguration>(config);
        services.AddIncidentReporting(config);
        using var provider = services.BuildServiceProvider();
        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<AgentPlatformOptions>>().Value);
    }

    private static IConfigurationRoot Configuration(bool enabled = true, bool autoReport = true) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AgentPlatform:Enabled"] = enabled.ToString(),
            ["AgentPlatform:AutoReportIncidents"] = autoReport.ToString(),
            ["AgentPlatform:ControlPlaneUrl"] = "https://control-plane.test",
            ["AgentPlatform:ProjectCode"] = "ROOM",
            ["AgentPlatform:Environment"] = "Production",
            ["AgentPlatform:RequestTimeoutSeconds"] = "10"
        }).Build();

    private static async Task<WebApplication> StartAppAsync(string environment, RecordingHandler handler,
        bool enabled = true, bool autoReport = true, string? controlPlaneUrl = null)
    {
        // Use the real incident services and compiled MVC views without Program's DB migration/seeding.
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(HomeController).Assembly.GetName().Name,
            EnvironmentName = environment
        });
        builder.WebHost.UseTestServer();
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddConfiguration(Configuration(enabled, autoReport));
        if (controlPlaneUrl is not null)
            builder.Configuration["AgentPlatform:ControlPlaneUrl"] = controlPlaneUrl;
        builder.Services.AddControllersWithViews().AddApplicationPart(typeof(HomeController).Assembly);
        builder.Services.AddIncidentReporting(builder.Configuration);
        builder.Services.AddHttpClient(IncidentReporter.HttpClientName).ConfigurePrimaryHttpMessageHandler(() => handler);
        var app = builder.Build();
        if (!app.Environment.IsDevelopment())
            app.UseExceptionHandler("/Home/Error");
        app.UseMiddleware<IncidentReportingMiddleware>();
        app.MapControllerRoute("default", "{controller=Dashboard}/{action=Index}/{id?}");
        app.MapDevelopmentIncidentTestEndpoint();
        app.MapGet("/boom", () => ThrowUnexpected());
        await app.StartAsync();
        return app;
    }

    private static IResult ThrowUnexpected() => throw new InvalidOperationException("Unexpected application failure");
}
