using BenchmarkDotNet.Attributes;

namespace Incursa.OpenAI.ChatKit.Benchmarks;

/// <summary>
/// Benchmarks the request serialization and deserialization boundary used by ChatKit entrypoints.
/// </summary>
[MemoryDiagnoser]
public class ChatKitJsonBenchmarks
{
    private byte[] threadsCreateRequestBytes = [];
    private byte[] threadsGetByIdRequestBytes = [];

    /// <summary>
    /// Precomputes representative request payloads for the deserialize benchmarks.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        threadsCreateRequestBytes = ChatKitJson.SerializeToUtf8Bytes<ChatKitRequest>(ChatKitBenchmarkData.ThreadsCreateRequest);
        threadsGetByIdRequestBytes = ChatKitJson.SerializeToUtf8Bytes<ChatKitRequest>(ChatKitBenchmarkData.ThreadsGetByIdRequest);
    }

    /// <summary>
    /// Measures serialization of a representative streaming request.
    /// </summary>
    [Benchmark]
    public byte[] SerializeThreadsCreateRequest()
        => ChatKitJson.SerializeToUtf8Bytes<ChatKitRequest>(ChatKitBenchmarkData.ThreadsCreateRequest);

    /// <summary>
    /// Measures deserialization of a representative streaming request.
    /// </summary>
    [Benchmark]
    public ChatKitRequest DeserializeThreadsCreateRequest()
        => ChatKitJson.DeserializeRequest(threadsCreateRequestBytes);

    /// <summary>
    /// Measures serialization of a representative non-streaming request.
    /// </summary>
    [Benchmark]
    public byte[] SerializeThreadsGetByIdRequest()
        => ChatKitJson.SerializeToUtf8Bytes<ChatKitRequest>(ChatKitBenchmarkData.ThreadsGetByIdRequest);

    /// <summary>
    /// Measures deserialization of a representative non-streaming request.
    /// </summary>
    [Benchmark]
    public ChatKitRequest DeserializeThreadsGetByIdRequest()
        => ChatKitJson.DeserializeRequest(threadsGetByIdRequestBytes);
}
