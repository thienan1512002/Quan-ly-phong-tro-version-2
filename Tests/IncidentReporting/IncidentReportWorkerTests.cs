using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using QuanLyPhongTro.Infrastructure.IncidentReporting;

namespace QuanLyPhongTro.Tests.IncidentReporting;

public sealed class IncidentReportWorkerTests
{
    [Fact]
    public async Task Duplicate_is_suppressed_for_ten_seconds_then_reported_again()
    {
        var options = Options.Create(TestIncidents.Options());
        var queue = new IncidentReportQueue(options);
        var reporter = new RecordingReporter();
        var clock = new TestClock();
        using var worker = Worker(queue, reporter, options, clock);
        await worker.StartAsync(CancellationToken.None);
        try
        {
            Assert.True(queue.TryEnqueue(TestIncidents.Payload("first")));
            Assert.Equal("first", (await reporter.NextAsync()).CorrelationId);
            // A barrier guarantees that the prior ReportAsync has completed and the duplicate was consumed.
            Assert.True(queue.TryEnqueue(TestIncidents.Payload("barrier-1") with { Endpoint = "/barrier-1" }));
            Assert.Equal("barrier-1", (await reporter.NextAsync()).CorrelationId);
            clock.Advance(TimeSpan.FromSeconds(9));
            Assert.True(queue.TryEnqueue(TestIncidents.Payload("duplicate")));
            Assert.True(queue.TryEnqueue(TestIncidents.Payload("barrier-2") with { Endpoint = "/barrier-2" }));
            Assert.Equal("barrier-2", (await reporter.NextAsync()).CorrelationId);

            clock.Advance(TimeSpan.FromSeconds(1));
            Assert.True(queue.TryEnqueue(TestIncidents.Payload("after-window")));
            Assert.Equal("after-window", (await reporter.NextAsync()).CorrelationId);
        }
        finally { await worker.StopAsync(CancellationToken.None); }
    }

    [Theory]
    [InlineData("type")]
    [InlineData("endpoint")]
    [InlineData("frame")]
    public async Task Fingerprint_distinguishes_type_endpoint_and_first_relevant_frame(string change)
    {
        var options = Options.Create(TestIncidents.Options());
        var queue = new IncidentReportQueue(options);
        var reporter = new RecordingReporter();
        using var worker = Worker(queue, reporter, options, new TestClock());
        await worker.StartAsync(CancellationToken.None);
        try
        {
            var first = TestIncidents.Payload("first");
            var second = TestIncidents.Payload("second");
            second = change switch
            {
                "type" => second with { ErrorType = "System.NullReferenceException" },
                "endpoint" => second with { Endpoint = "/Room/Edit" },
                _ => second with { StackTrace = "at System.Runtime.Throw()\n at QuanLyPhongTro.Controllers.RoomController.Edit()" }
            };
            Assert.True(queue.TryEnqueue(first));
            Assert.True(queue.TryEnqueue(second));
            Assert.Equal("first", (await reporter.NextAsync()).CorrelationId);
            Assert.Equal("second", (await reporter.NextAsync()).CorrelationId);
        }
        finally { await worker.StopAsync(CancellationToken.None); }
    }

    [Fact]
    public async Task Framework_frames_and_error_message_do_not_defeat_the_throttle()
    {
        var options = Options.Create(TestIncidents.Options());
        var queue = new IncidentReportQueue(options);
        var reporter = new RecordingReporter();
        using var worker = Worker(queue, reporter, options, new TestClock());
        await worker.StartAsync(CancellationToken.None);
        try
        {
            Assert.True(queue.TryEnqueue(TestIncidents.Payload("first")));
            Assert.True(queue.TryEnqueue(TestIncidents.Payload("duplicate") with
            {
                ErrorMessage = "Different message",
                StackTrace = "at Microsoft.AspNetCore.Internal.Throw()\n at QuanLyPhongTro.Controllers.RoomController.Index() in RoomController.cs:line 20"
            }));
            Assert.True(queue.TryEnqueue(TestIncidents.Payload("barrier") with { Endpoint = "/barrier" }));
            Assert.Equal("first", (await reporter.NextAsync()).CorrelationId);
            Assert.Equal("barrier", (await reporter.NextAsync()).CorrelationId);
        }
        finally { await worker.StopAsync(CancellationToken.None); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Failed_report_is_not_throttled_and_worker_survives_reporter_errors(bool throws)
    {
        var options = Options.Create(TestIncidents.Options());
        var queue = new IncidentReportQueue(options);
        var reporter = new RecordingReporter
        {
            OnReport = incident => incident.CorrelationId == "first"
                ? throws ? throw new InvalidOperationException("Reporter failure") : false
                : true
        };
        using var worker = Worker(queue, reporter, options, new TestClock());
        await worker.StartAsync(CancellationToken.None);
        try
        {
            Assert.True(queue.TryEnqueue(TestIncidents.Payload("first")));
            Assert.True(queue.TryEnqueue(TestIncidents.Payload("second")));
            Assert.Equal("first", (await reporter.NextAsync()).CorrelationId);
            Assert.Equal("second", (await reporter.NextAsync()).CorrelationId);
            Assert.False(worker.ExecuteTask!.IsCompleted);
        }
        finally { await worker.StopAsync(CancellationToken.None); }
    }

    [Fact]
    public async Task Empty_queue_can_be_stopped_cleanly()
    {
        var options = Options.Create(TestIncidents.Options());
        using var worker = Worker(new IncidentReportQueue(options), new RecordingReporter(), options, new TestClock());
        await worker.StartAsync(CancellationToken.None);
        await worker.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(1));
        Assert.True(worker.ExecuteTask!.IsCompleted);
    }

    private static IncidentReportWorker Worker(IncidentReportQueue queue, IIncidentReporter reporter,
        IOptions<AgentPlatformOptions> options, TimeProvider clock) =>
        new(queue, reporter, options, clock, NullLogger<IncidentReportWorker>.Instance);
}
