using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using QuanLyPhongTro.Infrastructure.IncidentReporting;

namespace QuanLyPhongTro.Tests.IncidentReporting;

public sealed class IncidentReportingMiddlewareTests
{
    [Fact]
    public async Task Unhandled_exception_is_enqueued_and_the_original_exception_is_rethrown()
    {
        var exception = new InvalidOperationException("Unexpected failure");
        var options = TestIncidents.Options();
        options.DeploymentVersion = "v2.1";
        options.GitCommit = "abc123";
        var queue = new IncidentReportQueue(Options.Create(options));
        var context = Context();
        context.Request.QueryString = new QueryString("?private=value");
        var middleware = Middleware(_ => throw exception, queue, options);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));
        Assert.Same(exception, actual);
        var incident = await NextAsync(queue);
        Assert.Equal("ROOM", incident.ProjectCode);
        Assert.Equal("Application", incident.Source);
        Assert.Equal("Production", incident.Environment);
        Assert.Equal("/Room/Index", incident.Endpoint);
        Assert.Equal("trace-123", incident.CorrelationId);
        Assert.Equal(typeof(InvalidOperationException).FullName, incident.ErrorType);
        Assert.Equal(exception.Message, incident.ErrorMessage);
        Assert.Contains(nameof(Unhandled_exception_is_enqueued_and_the_original_exception_is_rethrown), incident.StackTrace);
        Assert.Equal("v2.1", incident.DeploymentVersion);
        Assert.Equal("abc123", incident.GitCommit);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public async Task Either_disabled_switch_preserves_exception_without_enqueuing(bool enabled, bool autoReport)
    {
        var options = TestIncidents.Options();
        options.Enabled = enabled;
        options.AutoReportIncidents = autoReport;
        var queue = new IncidentReportQueue(Options.Create(options));
        var exception = new InvalidOperationException("Unexpected failure");
        var middleware = Middleware(_ => throw exception, queue, options);

        Assert.Same(exception, await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(Context())));
        await AssertEmptyAsync(queue);
    }

    [Fact]
    public async Task Successful_request_keeps_status_body_and_does_not_enqueue()
    {
        var options = TestIncidents.Options();
        var queue = new IncidentReportQueue(Options.Create(options));
        var context = Context();
        context.Response.Body = new MemoryStream();
        var middleware = Middleware(async ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status201Created;
            await ctx.Response.WriteAsync("existing response");
        }, queue, options);

        await middleware.InvokeAsync(context);
        Assert.Equal(201, context.Response.StatusCode);
        Assert.Equal("existing response", Encoding.UTF8.GetString(((MemoryStream)context.Response.Body).ToArray()));
        await AssertEmptyAsync(queue);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Expected_cancellation_is_rethrown_without_reporting(bool taskCanceled)
    {
        Exception exception = taskCanceled ? new TaskCanceledException() : new OperationCanceledException();
        var options = TestIncidents.Options();
        var queue = new IncidentReportQueue(Options.Create(options));
        var middleware = Middleware(_ => throw exception, queue, options);

        Assert.Same(exception, await Assert.ThrowsAnyAsync<OperationCanceledException>(() => middleware.InvokeAsync(Context())));
        await AssertEmptyAsync(queue);
    }

    [Fact]
    public async Task Full_queue_drops_new_incident_without_waiting_or_replacing_original_exception()
    {
        var options = TestIncidents.Options();
        options.QueueCapacity = 1;
        var queue = new IncidentReportQueue(Options.Create(options));
        Assert.True(queue.TryEnqueue(TestIncidents.Payload("first")));
        var exception = new InvalidOperationException("Unexpected failure");
        var logger = new RecordingLogger<IncidentReportingMiddleware>();
        var middleware = Middleware(_ => throw exception, queue, options, logger: logger);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            middleware.InvokeAsync(Context()).WaitAsync(TimeSpan.FromSeconds(1)));
        Assert.Same(exception, actual);
        Assert.Equal("first", (await NextAsync(queue)).CorrelationId);
        Assert.Contains(logger.Entries, entry => entry.Message.Contains("queue is full"));
    }

    [Fact]
    public async Task Capture_failure_cannot_mask_the_application_exception()
    {
        var options = TestIncidents.Options();
        var queue = new IncidentReportQueue(Options.Create(options));
        var exception = new InvalidOperationException("Original application failure");
        var logger = new RecordingLogger<IncidentReportingMiddleware>();
        var middleware = Middleware(_ => throw exception, queue, options, filter: new ThrowingFilter(), logger: logger);

        Assert.Same(exception, await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(Context())));
        Assert.Contains(logger.Entries, entry => entry.Message.Contains("capture failed"));
        await AssertEmptyAsync(queue);
    }

    [Fact]
    public async Task Payload_redacts_credentials_and_personal_data_and_never_reads_the_request_body()
    {
        const string connection = "Server=private-server;Database=ROOM;User Id=private-user;Password=db-secret";
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = connection,
            ["GitHub:Token"] = "configured-secret-token"
        }).Build();
        var options = TestIncidents.Options();
        var queue = new IncidentReportQueue(Options.Create(options));
        var context = Context();
        context.Request.Headers.Authorization = "Bearer request-auth-secret";
        context.Request.Headers.Cookie = "session=request-cookie-secret";
        context.Request.QueryString = new QueryString("?token=query-secret&password=query-password&ordinary=not-to-send");
        var body = new MemoryStream(Encoding.UTF8.GetBytes("{\"password\":\"body-password\",\"name\":\"body-private-person\"}"));
        context.Request.Body = body;
        var exception = new InvalidOperationException(
            $"{connection}; db-secret; configured-secret-token; request-auth-secret; request-cookie-secret; " +
            "query-secret; query-password; password=unconfigured-password; token=unconfigured-token; " +
            "user@example.com; 0912345678; Bearer unconfigured-bearer; https://alice:uri-password@host.test");
        var middleware = Middleware(_ => throw exception, queue, options, configuration: config);

        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));
        var incident = await NextAsync(queue);
        var json = JsonSerializer.Serialize(incident);
        foreach (var secret in new[] { connection, "db-secret", "configured-secret-token", "request-auth-secret",
                     "request-cookie-secret", "query-secret", "query-password", "unconfigured-password", "unconfigured-token",
                     "user@example.com", "0912345678", "unconfigured-bearer", "uri-password", "body-password",
                     "body-private-person", "not-to-send" })
            Assert.DoesNotContain(secret, json);
        Assert.Contains("[REDACTED]", incident.ErrorMessage);
        Assert.Equal("/Room/Index", incident.Endpoint);
        Assert.Equal(0, body.Position);
    }

    [Fact]
    public async Task Payload_respects_control_plane_field_limits()
    {
        var options = TestIncidents.Options();
        options.DeploymentVersion = new string('v', 300);
        options.GitCommit = new string('g', 200);
        var queue = new IncidentReportQueue(Options.Create(options));
        var context = Context();
        context.Request.Path = "/" + new string('a', 3000);
        context.TraceIdentifier = new string('c', 300);
        var middleware = Middleware(_ => throw new InvalidOperationException(new string('e', 6000)), queue, options);

        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));
        var incident = await NextAsync(queue);
        Assert.Equal(256, incident.Title.Length);
        Assert.Equal(4096, incident.ErrorMessage.Length);
        Assert.Equal(2048, incident.Endpoint.Length);
        Assert.Equal(256, incident.CorrelationId.Length);
        Assert.Equal(256, incident.DeploymentVersion!.Length);
        Assert.Equal(128, incident.GitCommit!.Length);
    }

    [Theory]
    [InlineData("password=\"two word password\" trailing", "two word password")]
    [InlineData("{\"password\":\"secret with \\\"escaped\\\" quote\"}", "secret with")]
    [InlineData("Password='complex; secret';Database=private-db", "complex; secret")]
    [InlineData("Authorization: Basic YWxpY2U6c2VjcmV0", "YWxpY2U6c2VjcmV0")]
    [InlineData("eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJwcml2YXRlIn0.c2lnbmF0dXJl", "eyJhbGciOiJIUzI1NiJ9")]
    [InlineData("clientSecret=private-secret", "private-secret")]
    [InlineData("https://alice:p%40ssword@host.test/resource", "p%40ssword")]
    public void Diagnostic_text_redacts_common_secret_formats(string text, string secret)
    {
        var sanitizer = new IncidentDataSanitizer(new ConfigurationBuilder().Build(), NullLogger<IncidentDataSanitizer>.Instance);
        var sanitized = sanitizer.Sanitize(text, 4096, Context());
        Assert.DoesNotContain(secret, sanitized);
        Assert.Contains("[REDACTED]", sanitized);
    }

    private static DefaultHttpContext Context()
    {
        var context = new DefaultHttpContext { TraceIdentifier = "trace-123" };
        context.Request.Path = "/Room/Index";
        return context;
    }

    private static IncidentReportingMiddleware Middleware(RequestDelegate next, IncidentReportQueue queue,
        AgentPlatformOptions options, IConfiguration? configuration = null, IIncidentReportFilter? filter = null,
        RecordingLogger<IncidentReportingMiddleware>? logger = null) => new(next, queue,
        filter ?? new DefaultIncidentReportFilter(),
        new IncidentDataSanitizer(configuration ?? new ConfigurationBuilder().Build(), NullLogger<IncidentDataSanitizer>.Instance),
        Options.Create(options), logger ?? (Microsoft.Extensions.Logging.ILogger<IncidentReportingMiddleware>)NullLogger<IncidentReportingMiddleware>.Instance);

    private static async Task<IncidentPayload> NextAsync(IncidentReportQueue queue)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await using var reader = queue.ReadAllAsync(timeout.Token).GetAsyncEnumerator();
        Assert.True(await reader.MoveNextAsync());
        return reader.Current;
    }

    private static async Task AssertEmptyAsync(IncidentReportQueue queue)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        await using var reader = queue.ReadAllAsync(timeout.Token).GetAsyncEnumerator();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => reader.MoveNextAsync().AsTask());
    }

    private sealed class ThrowingFilter : IIncidentReportFilter
    {
        public bool ShouldReport(Exception exception, HttpContext context) => throw new InvalidOperationException("Filter failure");
    }
}
