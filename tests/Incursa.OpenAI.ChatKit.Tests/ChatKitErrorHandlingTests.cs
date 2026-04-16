using System.Text;

namespace Incursa.OpenAI.ChatKit.Tests;

[Trait("Category", "Unit")]
public sealed class ChatKitErrorHandlingTests
{
    /// <summary>Custom stream failures surface the default custom error code and preserve retry/message semantics.</summary>
    /// <intent>Protect public stream error behavior for application-defined failures that intentionally use the default ChatKit custom code.</intent>
    /// <scenario>LIB-CHATKIT-CORE-003</scenario>
    /// <behavior>When a response stream throws a custom stream exception, the emitted error event uses the custom code and preserves the configured retry and message values.</behavior>
    [Trait("Category", "Negative")]
    [Fact]
    public async Task ProcessAsync_EmitsCustomErrorEvent_WhenRespondAsyncThrowsCustomStreamException()
    {
        ErrorServer server = new(static () => new CustomStreamException("try again later", allowRetry: true));

        ErrorEvent error = await RunThreadsCreateAndGetErrorAsync(server);

        Assert.Equal("custom", error.Code);
        Assert.Equal("try again later", error.Message);
        Assert.True(error.AllowRetry);
    }

    /// <summary>Explicit stream failures surface their configured protocol code and preserve retry/message semantics.</summary>
    /// <intent>Protect public stream error behavior for failures that intentionally choose a non-default protocol code.</intent>
    /// <scenario>LIB-CHATKIT-CORE-003</scenario>
    /// <behavior>When a response stream throws a stream exception, the emitted error event uses the configured code and preserves the configured retry and message values.</behavior>
    [Trait("Category", "Negative")]
    [Fact]
    public async Task ProcessAsync_EmitsExplicitErrorEvent_WhenRespondAsyncThrowsStreamException()
    {
        ErrorServer server = new(static () => new StreamException("rate_limited", allowRetry: false, message: "back off"));

        ErrorEvent error = await RunThreadsCreateAndGetErrorAsync(server);

        Assert.Equal("rate_limited", error.Code);
        Assert.Equal("back off", error.Message);
        Assert.False(error.AllowRetry);
    }

    /// <summary>Unexpected stream failures collapse to the canonical runtime stream error code with retry enabled.</summary>
    /// <intent>Protect public stream error behavior when an unhandled runtime failure escapes the response stream.</intent>
    /// <scenario>LIB-CHATKIT-CORE-003</scenario>
    /// <behavior>When a response stream throws an unexpected exception, the emitted error event uses the canonical stream error code and allows retry.</behavior>
    [Trait("Category", "Negative")]
    [Fact]
    public async Task ProcessAsync_EmitsCanonicalStreamError_WhenRespondAsyncThrowsUnexpectedException()
    {
        ErrorServer server = new(static () => new InvalidOperationException("boom"));

        ErrorEvent error = await RunThreadsCreateAndGetErrorAsync(server);

        Assert.Equal(ErrorCodes.StreamError, error.Code);
        Assert.Null(error.Message);
        Assert.True(error.AllowRetry);
    }

    /// <summary>The stream exception types preserve the configured public code, retry, and message values.</summary>
    /// <intent>Protect the exception contracts that the runtime translates into public ChatKit error events.</intent>
    /// <scenario>LIB-CHATKIT-CORE-002</scenario>
    /// <behavior>Constructed stream exception types expose the expected code, retry, and message property values.</behavior>
    [Trait("Category", "Positive")]
    [Fact]
    public void StreamExceptionTypes_PreserveConfiguredProperties()
    {
        StreamException explicitException = new("rate_limited", allowRetry: true, message: "wait");
        CustomStreamException customException = new("custom message", allowRetry: false);

        Assert.Equal("rate_limited", explicitException.Code);
        Assert.True(explicitException.AllowRetry);
        Assert.Equal("wait", explicitException.Message);
        Assert.False(customException.AllowRetry);
        Assert.Equal("custom message", customException.Message);
    }

    private static async Task<ErrorEvent> RunThreadsCreateAndGetErrorAsync(ErrorServer server)
    {
        ThreadsCreateRequest request = new()
        {
            Params = new ThreadCreateParams
            {
                Input = new UserMessageInput
                {
                    Content =
                    [
                        new UserMessageTextContent { Text = "hello" },
                    ],
                },
            },
        };

        ChatKitProcessResult result = await server.ProcessAsync(
            ChatKitJson.SerializeToUtf8Bytes<ChatKitRequest>(request),
            new Dictionary<string, object?>());

        StreamingResult streaming = Assert.IsType<StreamingResult>(result);
        await foreach (byte[] chunk in streaming)
        {
            string text = Encoding.UTF8.GetString(chunk);
            if (!text.StartsWith("data: ", StringComparison.Ordinal))
            {
                continue;
            }

            string payload = text["data: ".Length..].Trim();
            ThreadStreamEvent @event = Assert.IsAssignableFrom<ThreadStreamEvent>(ChatKitJson.Deserialize<ThreadStreamEvent>(payload)!);
            if (@event is ErrorEvent error)
            {
                return error;
            }
        }

        throw new Xunit.Sdk.XunitException("Expected an error event in the streamed response.");
    }

    private sealed class ErrorServer : ChatKitServer<Dictionary<string, object?>>
    {
        private readonly Func<Exception> exceptionFactory;

        public ErrorServer(Func<Exception> exceptionFactory)
            : base(new InMemoryChatKitStore<Dictionary<string, object?>>())
        {
            this.exceptionFactory = exceptionFactory;
        }

        public override async IAsyncEnumerable<ThreadStreamEvent> RespondAsync(
            ThreadMetadata thread,
            UserMessageItem? inputUserMessage,
            Dictionary<string, object?> context,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            _ = thread;
            _ = inputUserMessage;
            _ = context;
            await Task.Yield();
            if (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }

            throw exceptionFactory();
        }
    }
}
