using System.Diagnostics;
using System.Text.Json;

namespace Incursa.OpenAI.ChatKit.Tests;

public sealed class UpstreamSyncScriptTests
{
    /// <summary>The upstream sync state helper bootstraps ChatKit-specific tracked and local state defaults.</summary>
    /// <intent>Protect the repeatable initialization path for the upstream synchronization workflow.</intent>
    /// <scenario>REPO-SYNC-STATE-001</scenario>
    /// <behavior>Initializing sync state emits ChatKit upstream metadata and creates both tracked and local state files when missing.</behavior>
    [Fact]
    public async Task InitializeUpstreamSyncState_CreatesChatKitDefaults()
    {
        using TemporaryDirectory temp = new();
        string trackedPath = Path.Combine(temp.Path, "state.json");
        string localPath = Path.Combine(temp.Path, "state.local.json");

        string json = await InvokePowerShellAsync(
            $$"""
            . '{{EscapePowerShellLiteral(GetCommonScriptPath())}}'
            $state = Initialize-UpstreamSyncState `
                -TrackedStatePath '{{EscapePowerShellLiteral(trackedPath)}}' `
                -LocalStatePath '{{EscapePowerShellLiteral(localPath)}}' `
                -UpstreamPath 'C:\src\openai\chatkit-python' `
                -UpstreamBranch 'main'
            $state | ConvertTo-Json -Depth 8 -Compress
            """);

        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement tracked = document.RootElement.GetProperty("tracked");
        JsonElement local = document.RootElement.GetProperty("local");

        Assert.Equal("https://github.com/openai/chatkit-python.git", tracked.GetProperty("upstreamRepoUrl").GetString());
        Assert.Equal("https://github.com/incursa/chatkit-dotnet.git", tracked.GetProperty("targetRepoUrl").GetString());
        Assert.Equal(@"C:\src\openai\chatkit-python", tracked.GetProperty("upstreamLocalPath").GetString());
        Assert.Equal("main", tracked.GetProperty("upstreamBranch").GetString());
        Assert.True(local.TryGetProperty("bootstrapLastTranslatedSha", out _));
        Assert.True(File.Exists(trackedPath));
        Assert.True(File.Exists(localPath));
    }

    /// <summary>The upstream sync prompt builder carries the ChatKit translation constraints and trims oversized diff content.</summary>
    /// <intent>Protect the Codex prompt contract used for automated upstream translation runs.</intent>
    /// <scenario>REPO-SYNC-PROMPT-001</scenario>
    /// <behavior>Building a prompt includes the ChatKit guidance and notes content while truncating large diff excerpts.</behavior>
    [Fact]
    public async Task NewTranslationPrompt_UsesChatKitConstraintsAndTruncatesDiff()
    {
        using TemporaryDirectory temp = new();
        string guidancePath = Path.Combine(temp.Path, "AGENTS.md");
        string notesPath = Path.Combine(temp.Path, "CODEX_TRANSLATION_NOTES.md");
        await File.WriteAllTextAsync(guidancePath, "Repo guidance line.");
        await File.WriteAllTextAsync(notesPath, "Notes line.");

        string largeDiff = new('x', 6500);
        string prompt = await InvokePowerShellAsync(
            $$"""
            . '{{EscapePowerShellLiteral(GetCommonScriptPath())}}'
            $prompt = New-TranslationPrompt `
                -BaseSha 'abc1234' `
                -LatestSha 'def5678' `
                -CommitLines @('abc1234 add request mapping') `
                -DiffLines @('{{largeDiff}}') `
                -ChangedFiles @('src/chatkit/server.py') `
                -UpstreamRepoUrl 'https://github.com/openai/chatkit-python.git' `
                -UpstreamBranch 'main' `
                -GuidancePath '{{EscapePowerShellLiteral(guidancePath)}}' `
                -CodexNotesPath '{{EscapePowerShellLiteral(notesPath)}}'
            $prompt
            """);

        Assert.Contains("https://github.com/openai/chatkit-python.git", prompt, StringComparison.Ordinal);
        Assert.Contains("Preserve exact ChatKit wire compatibility", prompt, StringComparison.Ordinal);
        Assert.Contains("Repo guidance line.", prompt, StringComparison.Ordinal);
        Assert.Contains("Notes line.", prompt, StringComparison.Ordinal);
        Assert.Contains("... [truncated at 6000 characters] ...", prompt, StringComparison.Ordinal);
    }

    /// <summary>The upstream sync metadata helpers produce ChatKit-specific branch, commit, and pull request text.</summary>
    /// <intent>Protect predictable naming for automated sync branches and PRs.</intent>
    /// <scenario>REPO-SYNC-METADATA-001</scenario>
    /// <behavior>Generating sync metadata returns the expected ChatKit branch name, commit text, and commit summary body.</behavior>
    [Fact]
    public async Task SyncMetadataHelpers_UseChatKitNaming()
    {
        string json = await InvokePowerShellAsync(
            $$"""
            . '{{EscapePowerShellLiteral(GetCommonScriptPath())}}'
            $result = [ordered]@{
                branch = Get-SyncBranchName -LatestSha '0123456789abcdef'
                commitMessage = Get-SyncCommitMessage -LatestSha '0123456789abcdef'
                title = Get-SyncPullRequestTitle -LatestSha '0123456789abcdef'
                body = Get-SyncPullRequestBody `
                    -UpstreamRepoUrl 'https://github.com/openai/chatkit-python.git' `
                    -BaseSha 'abc1234' `
                    -LatestSha '0123456789abcdef' `
                    -CommitLines @('abc1234 add widget diff', 'def5678 add endpoint test')
            }
            $result | ConvertTo-Json -Compress
            """);

        using JsonDocument document = JsonDocument.Parse(json);
        Assert.Equal("sync/chatkit-upstream-0123456", document.RootElement.GetProperty("branch").GetString());
        Assert.Equal("Sync upstream chatkit-python through 0123456", document.RootElement.GetProperty("commitMessage").GetString());
        Assert.Equal("Sync upstream chatkit-python through 0123456", document.RootElement.GetProperty("title").GetString());

        string body = document.RootElement.GetProperty("body").GetString()!;
        Assert.Contains("- abc1234 add widget diff", body, StringComparison.Ordinal);
        Assert.Contains("- def5678 add endpoint test", body, StringComparison.Ordinal);
    }

    private static string GetCommonScriptPath()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "tools", "upstream-sync", "UpstreamSync.Common.ps1"));

    private static string EscapePowerShellLiteral(string value)
        => value.Replace("'", "''", StringComparison.Ordinal);

    private static async Task<string> InvokePowerShellAsync(string script)
    {
        using TemporaryDirectory temp = new();
        string scriptPath = Path.Combine(temp.Path, "test.ps1");
        await File.WriteAllTextAsync(
            scriptPath,
            """
            $ErrorActionPreference = 'Stop'
            Set-StrictMode -Version Latest
            """ + Environment.NewLine + script);

        ProcessStartInfo startInfo = new()
        {
            FileName = "pwsh",
            Arguments = $"-NoProfile -File \"{scriptPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start pwsh.");

        string stdout = await process.StandardOutput.ReadToEndAsync();
        string stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new Xunit.Sdk.XunitException($"pwsh exited with code {process.ExitCode}.{Environment.NewLine}STDOUT:{Environment.NewLine}{stdout}{Environment.NewLine}STDERR:{Environment.NewLine}{stderr}");
        }

        return stdout.Trim();
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "chatkit-dotnet-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
