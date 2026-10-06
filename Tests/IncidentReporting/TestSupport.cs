using System.Collections.Concurrent;
using System.Net;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using QuanLyPhongTro.Infrastructure.IncidentReporting;

namespace QuanLyPhongTro.Tests.IncidentReporting;

internal sealed class TestClock : TimeProvider
{
    private long _timestamp;
    public ConcurrentQueue<TimeSpan> Delays { get; } = new();
    public override long TimestampFrequency => TimeSpan.TicksPerSecond;
    public override long GetTimestamp() => Interlocked.Read(ref _timestamp);
    public void Advance(TimeSpan duration) => Interlocked.Add(ref _timestamp, duration.Ticks);

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        Delays.Enqueue(dueTime);
        // Execute delays promptly while recording the requested backoff duration.
        return base.CreateTimer(callback, state, TimeSpan.Zero, period);
    }
}

internal sealed class RecordingReporter : IIncidentReporter
{
    private readonly Channel<IncidentPayload> _attempts = Channel.CreateUnbounded<IncidentPayload>();
    public Func<IncidentPayload, bool> OnReport { get; set; } = _ => true;

    public Task<bool> ReportAsync(IncidentPayload incident, CancellationToken cancellationToken)
    {
        _attempts.Writer.TryWrite(incident);
        return Task.FromResult(OnReport(incident));
    }

    public async Task<IncidentPayload> NextAsync() =>
        await _attempts.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
}

internal sealed class RecordingHandler : HttpMessageHandler
{
    private readonly Channel<(Uri? Uri, string Json, HttpMethod Method, string? ContentType)> _requests =
        Channel.CreateUnbounded<(Uri?, string, HttpMethod, string?)>();
    private int _attemptCount;
    public int AttemptCount => Volatile.Read(ref _attemptCount);
    public Func<int, CancellationToken, Task<HttpResponseMessage>> OnSend { get; set; } =
        (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created));

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var count = Interlocked.Increment(ref _attemptCount);
        var json = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
        await _requests.Writer.WriteAsync((request.RequestUri, json, request.Method, request.Content?.Headers.ContentType?.MediaType), cancellationToken);
        return await OnSend(count, cancellationToken);
    }

    public async Task<(Uri? Uri, string Json, HttpMethod Method, string? ContentType)> NextAsync() =>
        await _requests.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
}

internal sealed class TestHttpClientFactory(RecordingHandler handler, TimeSpan? timeout = null) : IHttpClientFactory
{
    public int CreateCount { get; private set; }
    public HttpClient CreateClient(string name)
    {
        Assert.Equal(IncidentReporter.HttpClientName, name);
        CreateCount++;
        return new HttpClient(handler, disposeHandler: false)
        {
            BaseAddress = new Uri("https://control-plane.test/"),
            Timeout = timeout ?? TimeSpan.FromSeconds(10)
        };
    }
}

internal sealed class RecordingLogger<T> : ILogger<T>
{
    public ConcurrentQueue<(LogLevel Level, string Message)> Entries { get; } = new();
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter) => Entries.Enqueue((logLevel, formatter(state, exception)));
}

internal static class TestIncidents
{
    public static AgentPlatformOptions Options() => new()
    {
        Enabled = true,
        AutoReportIncidents = true,
        ProjectCode = "ROOM",
        Environment = "Production",
        ControlPlaneUrl = "https://control-plane.test"
    };

    public static IncidentPayload Payload(string correlationId = "trace-test") => new()
    {
        ProjectCode = "ROOM",
        Environment = "Production",
        Title = "Unhandled exception",
        ErrorType = "System.InvalidOperationException",
        ErrorMessage = "Test failure",
        Endpoint = "/Room/Index",
        CorrelationId = correlationId,
        StackTrace = "at System.Runtime.Throw()\n at QuanLyPhongTro.Controllers.RoomController.Index() in RoomController.cs:line 20"
    };
}
