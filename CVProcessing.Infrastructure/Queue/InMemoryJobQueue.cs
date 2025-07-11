using System.Collections.Concurrent;
using System.Threading.Channels;

namespace CVProcessing.Infrastructure.Queue;

/// <summary>
/// Implementación en memoria del sistema de colas usando Channels
/// </summary>
public class InMemoryJobQueue<T> : IJobQueue<T>
{
    private readonly Channel<T> _queue;
    private readonly ChannelWriter<T> _writer;
    private readonly ChannelReader<T> _reader;

    public InMemoryJobQueue(int capacity = 1000)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        };

        _queue = Channel.CreateBounded<T>(options);
        _writer = _queue.Writer;
        _reader = _queue.Reader;
    }

    public async Task EnqueueAsync(T job)
    {
        await _writer.WriteAsync(job);
    }

    public async Task<T?> DequeueAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _reader.ReadAsync(cancellationToken);
        }
        catch (InvalidOperationException)
        {
            // Channel was completed
            return default;
        }
    }

    public int Count => _reader.CanCount ? _reader.Count : 0;

    public void Complete()
    {
        _writer.Complete();
    }
}