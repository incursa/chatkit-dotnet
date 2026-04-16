# Incursa.OpenAI.ChatKit.Fuzz

This project contains the SharpFuzz harnesses for hostile ChatKit request and widget payloads.

## Purpose

- Feed arbitrary inputs into the ChatKit request deserializer.
- Exercise widget definition and encoded widget parsing against malformed payloads.
- Treat expected parse and validation failures as normal rejection paths instead of crashes.

## Build

```bash
dotnet build fuzz/Incursa.OpenAI.ChatKit.Fuzz.csproj -c Release
```

## Tooling

Run `dotnet tool restore` from the repo root to make the local `sharpfuzz` command available through the `SharpFuzz.CommandLine` tool package.
