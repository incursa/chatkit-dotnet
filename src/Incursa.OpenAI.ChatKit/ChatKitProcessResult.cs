namespace Incursa.OpenAI.ChatKit;

/// <summary>
/// Represents the result of processing a ChatKit request.
/// </summary>
public abstract class ChatKitProcessResult;

/// <summary>
/// Wraps a streaming ChatKit response encoded as server-sent-event payload chunks.
/// </summary>
public sealed class StreamingResult : ChatKitProcessResult, IAsyncEnumerable<byte[]>
{
    private readonly IAsyncEnumerable<byte[]> stream;

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamingResult"/> class.
    /// </summary>
    /// <param name="stream">The byte stream that should be forwarded to the HTTP response body.</param>
    public StreamingResult(IAsyncEnumerable<byte[]> stream)
    {
        this.stream = stream;
    }

    /// <summary>
    /// Returns an async enumerator over the streaming response chunks.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel enumeration.</param>
    /// <returns>An async enumerator over UTF-8 encoded SSE chunks.</returns>
    public IAsyncEnumerator<byte[]> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => stream.GetAsyncEnumerator(cancellationToken);
}

/// <summary>
/// Wraps a non-streaming ChatKit response encoded as a JSON payload.
/// </summary>
public sealed class NonStreamingResult : ChatKitProcessResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NonStreamingResult"/> class.
    /// </summary>
    /// <param name="json">The UTF-8 encoded JSON response payload.</param>
    public NonStreamingResult(byte[] json)
    {
        Json = json;
    }

    /// <summary>
    /// Gets the UTF-8 encoded JSON payload.
    /// </summary>
    public byte[] Json { get; }
}
