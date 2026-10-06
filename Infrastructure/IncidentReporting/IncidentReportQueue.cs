using System.Threading.Channels;
using Microsoft.Extensions.Options;

namespace QuanLyPhongTro.Infrastructure.IncidentReporting;

public sealed class IncidentReportQueue
{
    private readonly Channel<IncidentPayload> _channel;

    public IncidentReportQueue(IOptions<AgentPlatformOptions> options)
    {
        _channel = Channel.CreateBounded<IncidentPayload>(new BoundedChannelOptions(options.Value.QueueCapacity)
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false,
            FullMode = BoundedChannelFullMode.Wait
        });
    }

    // Never wait for capacity on an application request. A full queue returns false.
    public bool TryEnqueue(IncidentPayload incident) => _channel.Writer.TryWrite(incident);

    public IAsyncEnumerable<IncidentPayload> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
