using System.Text;
using System.Text.Json;
using SharpFuzz;

namespace Incursa.OpenAI.ChatKit.Fuzz;

public static class Program
{
    public static void Main(string[] args)
    {
        _ = args;
        Fuzzer.OutOfProcess.Run(ConsumeInput);
    }

    private static void ConsumeInput(Stream stream)
    {
        using MemoryStream buffer = new();
        stream.CopyTo(buffer);

        byte[] payload = buffer.ToArray();
        string text = Encoding.UTF8.GetString(payload);

        TryDeserializeRequest(payload);
        TryParseWidgetDefinition(text);
        TryParseEncodedWidget(payload);
    }

    private static void TryDeserializeRequest(byte[] payload)
    {
        try
        {
            _ = ChatKitJson.DeserializeRequest(payload);
        }
        catch (JsonException)
        {
        }
        catch (InvalidOperationException)
        {
        }
        catch (NotSupportedException)
        {
        }
    }

    private static void TryParseWidgetDefinition(string payload)
    {
        try
        {
            _ = WidgetDefinition.Parse(payload);
        }
        catch (JsonException)
        {
        }
        catch (InvalidOperationException)
        {
        }
        catch (NotSupportedException)
        {
        }
    }

    private static void TryParseEncodedWidget(byte[] payload)
    {
        try
        {
            _ = WidgetEncodedDefinition.Parse(ToBase64Url(payload));
        }
        catch (JsonException)
        {
        }
        catch (InvalidOperationException)
        {
        }
        catch (NotSupportedException)
        {
        }
    }

    private static string ToBase64Url(byte[] payload)
        => Convert.ToBase64String(payload).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
