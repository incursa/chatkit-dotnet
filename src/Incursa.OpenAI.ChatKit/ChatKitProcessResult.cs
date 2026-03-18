namespace Incursa.OpenAI.ChatKit;

public abstract class ChatKitProcessResult;

public sealed class StreamingResult : ChatKitProcessResult, IAsyncEnumerable<byte[]>
{
    private readonly IAsyncEnumerable<byte[]> stream;

    public StreamingResult(IAsyncEnumerable<byte[]> stream)
    {
        this.stream = stream;
    }

    public IAsyncEnumerator<byte[]> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => stream.GetAsyncEnumerator(cancellationToken);
}

public sealed class NonStreamingResult : ChatKitProcessResult
{
    public NonStreamingResult(byte[] json)
    {
        Json = json;
    }

    public byte[] Json { get; }
}
