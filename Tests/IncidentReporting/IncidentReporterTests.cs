using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuanLyPhongTro.Infrastructure.IncidentReporting;

namespace QuanLyPhongTro.Tests.IncidentReporting;

public sealed class IncidentReporterTests
{
    [Fact]
    public async Task Reports_using_the_expected_url_and_control_plane_JSON_contract()
    {
        using var handler = new RecordingHandler();
        var reporter = Reporter(handler);

        Assert.True(await reporter.ReportAsync(TestIncidents.Payload(), CancellationToken.None));
        var request = await handler.NextAsync();
        Assert.Equal("https://control-plane.test/api/incidents", request.Uri!.AbsoluteUri);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("application/json", request.ContentType);
        using var json = JsonDocument.Parse(request.Json);
        Assert.Equal("ROOM", json.RootElement.GetProperty("projectCode").GetString());
        Assert.Equal("Application", json.RootElement.GetProperty("source").GetString());
        Assert.Equal("Production", json.RootElement.GetProperty("environment").GetString());
        Assert.Equal("trace-test", json.RootElement.GetProperty("correlationId").GetString());
    }

    [Theory]
    [InlineData(500)]
    [InlineData(503)]
    [InlineData(408)]
    [InlineData(429)]
    public async Task Transient_HTTP_failure_is_retried_with_exponential_backoff(int status)
    {
        using var handler = new RecordingHandler
        {
            OnSend = (attempt, _) => Task.FromResult(new HttpResponseMessage(attempt < 3 ? (HttpStatusCode)status : HttpStatusCode.Created))
        };
        var clock = new TestClock();
        var reporter = Reporter(handler, clock: clock);

        Assert.True(await reporter.ReportAsync(TestIncidents.Payload(), CancellationToken.None));
        Assert.Equal(3, handler.AttemptCount);
        Assert.Equal(new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2) }, clock.Delays.ToArray());
    }

    [Theory]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(302)]
    public async Task Permanent_failure_and_redirect_are_not_retried(int status)
    {
        using var handler = new RecordingHandler
        {
            OnSend = (_, _) => Task.FromResult(new HttpResponseMessage((HttpStatusCode)status))
        };
        var clock = new TestClock();

        Assert.False(await Reporter(handler, clock: clock).ReportAsync(TestIncidents.Payload(), CancellationToken.None));
        Assert.Equal(1, handler.AttemptCount);
        Assert.Empty(clock.Delays);
    }

    [Fact]
    public async Task Unavailable_control_plane_stops_after_three_attempts_and_logs_no_secret_details()
    {
        using var handler = new RecordingHandler
        {
            OnSend = (_, _) => throw new HttpRequestException("password=do-not-log-me")
        };
        var logger = new RecordingLogger<IncidentReporter>();

        Assert.False(await Reporter(handler, logger: logger).ReportAsync(TestIncidents.Payload(), CancellationToken.None));
        Assert.Equal(3, handler.AttemptCount);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Warning && entry.Message.Contains("abandoned"));
        Assert.All(logger.Entries, entry => Assert.DoesNotContain("do-not-log-me", entry.Message));
    }

    [Fact]
    public async Task Configured_timeout_cancels_each_attempt_without_infinite_retries()
    {
        using var handler = new RecordingHandler
        {
            OnSend = async (_, token) =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
                return new HttpResponseMessage(HttpStatusCode.Created);
            }
        };
        var factory = new TestHttpClientFactory(handler, TimeSpan.FromMilliseconds(20));
        var reporter = new IncidentReporter(factory, Options.Create(TestIncidents.Options()), new TestClock(), new RecordingLogger<IncidentReporter>());

        Assert.False(await reporter.ReportAsync(TestIncidents.Payload(), CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Equal(3, handler.AttemptCount);
    }

    [Fact]
    public async Task Shutdown_cancellation_stops_retrying()
    {
        using var shutdown = new CancellationTokenSource();
        using var handler = new RecordingHandler
        {
            OnSend = (_, _) =>
            {
                shutdown.Cancel();
                return Task.FromCanceled<HttpResponseMessage>(shutdown.Token);
            }
        };

        Assert.False(await Reporter(handler).ReportAsync(TestIncidents.Payload(), shutdown.Token));
        Assert.Equal(1, handler.AttemptCount);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task Disabled_reporting_does_not_create_an_HTTP_client(bool enabled, bool autoReport)
    {
        using var handler = new RecordingHandler();
        var factory = new TestHttpClientFactory(handler);
        var options = TestIncidents.Options();
        options.Enabled = enabled;
        options.AutoReportIncidents = autoReport;
        var reporter = new IncidentReporter(factory, Options.Create(options), new TestClock(), new RecordingLogger<IncidentReporter>());

        Assert.False(await reporter.ReportAsync(TestIncidents.Payload(), CancellationToken.None));
        Assert.Equal(0, factory.CreateCount);
    }

    private static IncidentReporter Reporter(RecordingHandler handler, TestClock? clock = null,
        RecordingLogger<IncidentReporter>? logger = null) => new(new TestHttpClientFactory(handler),
        Options.Create(TestIncidents.Options()), clock ?? new TestClock(), logger ?? new RecordingLogger<IncidentReporter>());
}
