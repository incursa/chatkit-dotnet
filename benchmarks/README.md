# Benchmarks

This directory contains permanent BenchmarkDotNet suites for `Incursa.OpenAI.ChatKit`.

## Included Suites

- `ChatKitJsonBenchmarks`
- `WidgetStreamingBenchmarks`

## Run

```bash
dotnet run -c Release --project benchmarks/Incursa.OpenAI.ChatKit.Benchmarks.csproj -- --job Dry --filter "*ChatKitJsonBenchmarks*"
dotnet run -c Release --project benchmarks/Incursa.OpenAI.ChatKit.Benchmarks.csproj -- --job Dry --filter "*WidgetStreamingBenchmarks*"
```

Use `--filter` to narrow to a subset of benchmarks while iterating locally.
