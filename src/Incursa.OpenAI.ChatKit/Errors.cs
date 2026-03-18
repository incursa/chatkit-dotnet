namespace Incursa.OpenAI.ChatKit;

public static class ErrorCodes
{
    public const string StreamError = "stream_error";
}

public abstract class BaseStreamException : Exception
{
    protected BaseStreamException(string? message)
        : base(message)
    {
    }
}

public sealed class StreamException : BaseStreamException
{
    public StreamException(string code, bool allowRetry = false, string? message = null)
        : base(message)
    {
        Code = code;
        AllowRetry = allowRetry;
    }

    public string Code { get; }

    public bool AllowRetry { get; }
}

public sealed class CustomStreamException : BaseStreamException
{
    public CustomStreamException(string? message, bool allowRetry = false)
        : base(message)
    {
        AllowRetry = allowRetry;
    }

    public bool AllowRetry { get; }
}
