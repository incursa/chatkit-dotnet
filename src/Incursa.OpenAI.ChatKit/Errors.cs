namespace Incursa.OpenAI.ChatKit;

/// <summary>
/// Defines canonical error codes emitted by the ChatKit runtime.
/// </summary>
public static class ErrorCodes
{
    /// <summary>
    /// Identifies an unexpected server-side streaming failure.
    /// </summary>
    public const string StreamError = "stream_error";
}

/// <summary>
/// Serves as the base type for exceptions that should be translated into stream error events.
/// </summary>
public abstract class BaseStreamException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BaseStreamException"/> class.
    /// </summary>
    /// <param name="message">The optional error message sent back to the client.</param>
    protected BaseStreamException(string? message)
        : base(message)
    {
    }
}

/// <summary>
/// Represents a stream failure with an explicit protocol error code.
/// </summary>
public sealed class StreamException : BaseStreamException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StreamException"/> class.
    /// </summary>
    /// <param name="code">The error code surfaced to the client.</param>
    /// <param name="allowRetry">Indicates whether the client may retry the request.</param>
    /// <param name="message">The optional human-readable error message.</param>
    public StreamException(string code, bool allowRetry = false, string? message = null)
        : base(message)
    {
        Code = code;
        AllowRetry = allowRetry;
    }

    /// <summary>
    /// Gets the protocol error code returned to the client.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets a value indicating whether the client may retry the failed request.
    /// </summary>
    public bool AllowRetry { get; }
}

/// <summary>
/// Represents a custom stream failure that uses the default custom error code.
/// </summary>
public sealed class CustomStreamException : BaseStreamException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CustomStreamException"/> class.
    /// </summary>
    /// <param name="message">The optional human-readable error message.</param>
    /// <param name="allowRetry">Indicates whether the client may retry the request.</param>
    public CustomStreamException(string? message, bool allowRetry = false)
        : base(message)
    {
        AllowRetry = allowRetry;
    }

    /// <summary>
    /// Gets a value indicating whether the client may retry the failed request.
    /// </summary>
    public bool AllowRetry { get; }
}
