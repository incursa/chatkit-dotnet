using BenchmarkDotNet.Attributes;

namespace Incursa.OpenAI.ChatKit.Benchmarks;

/// <summary>
/// Benchmarks the widget diff path used for incremental streaming updates.
/// </summary>
[MemoryDiagnoser]
public class WidgetStreamingBenchmarks
{
    /// <summary>
    /// Measures the append-only streaming diff case that should produce a compact delta.
    /// </summary>
    [Benchmark]
    public IReadOnlyList<ThreadItemUpdate> DiffStreamingText()
        => WidgetStreaming.Diff(ChatKitBenchmarkData.BeforeStreamingWidget, ChatKitBenchmarkData.AfterStreamingWidget);

    /// <summary>
    /// Measures the incompatible widget update case that falls back to a root replacement.
    /// </summary>
    [Benchmark]
    public IReadOnlyList<ThreadItemUpdate> DiffFullReplace()
        => WidgetStreaming.Diff(ChatKitBenchmarkData.BeforeStreamingWidget, ChatKitBenchmarkData.FullReplaceWidget);
}
