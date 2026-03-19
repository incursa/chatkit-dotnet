using System.Diagnostics;

namespace Incursa.OpenAI.ChatKit.Tests;

/// <summary>Regression tests for the release versioning script.</summary>
public sealed class ReleaseVersioningScriptTests
{
    /// <summary>The release versioning script can advance from a tagged commit when the working tree is dirty.</summary>
    /// <intent>Protect release versioning for local release preparation on top of an already-tagged HEAD commit.</intent>
    /// <scenario>REPO-RELEASE-001</scenario>
    /// <behavior>When HEAD already has a release tag and tracked changes exist, the script continues instead of failing on the existing HEAD tag guard.</behavior>
    [Fact]
    public async Task InvokeReleaseVersioning_AllowsDirtyTreeOnTaggedHead_WhenTagCreationIsSkipped()
    {
        using TemporaryDirectory temp = new();
        await InitializeReleaseRepoAsync(temp.Path);

        string output = await InvokePowerShellAsync(
            temp.Path,
            """
            & ./scripts/release/Invoke-ReleaseVersioning.ps1 -Version 1.0.15 -NoCommit -NoTag -NoPush
            """);

        Assert.Contains("Current HEAD already has release tag(s): v1.0.14.", output, StringComparison.Ordinal);
        Assert.Contains("Dirty working tree detected; release versioning will continue from a new release commit.", output, StringComparison.Ordinal);
        Assert.Contains("Skipping commit because -NoCommit was specified.", output, StringComparison.Ordinal);
        Assert.Contains("Skipping tag because -NoTag was specified.", output, StringComparison.Ordinal);
    }

    /// <summary>The release versioning script can honor a manual minor bump override when the public API diff looks breaking.</summary>
    /// <intent>Protect the explicit bump override path for semver exceptions that are intentionally non-breaking.</intent>
    /// <scenario>REPO-RELEASE-002</scenario>
    /// <behavior>When the shipped API diff would normally require a major bump, the script respects -Bump Minor and the validator accepts it.</behavior>
    [Fact]
    public async Task InvokeReleaseVersioning_HonorsExplicitMinorBump_WhenApiDiffLooksBreaking()
    {
        using TemporaryDirectory temp = new();
        await InitializeReleaseRepoAsync(temp.Path);

        await File.WriteAllTextAsync(
            Path.Combine(temp.Path, "src", "Test.Library", "PublicAPI.Shipped.txt"),
            """
            #nullable enable
            namespace Test.Library
            {
                public sealed class Example
                {
                    public void DoWork() { }
                }
            }
            """);

        string output = await InvokePowerShellAsync(
            temp.Path,
            """
            & ./scripts/release/Invoke-ReleaseVersioning.ps1 -Bump Minor -NoCommit -NoTag -NoPush
            """);

        Assert.Contains("Required bump: Minor", output, StringComparison.Ordinal);
        Assert.Contains("Target release version: 1.1.0", output, StringComparison.Ordinal);
        Assert.Contains("Release policy check passed using explicit bump override: Minor.", output, StringComparison.Ordinal);
    }

    /// <summary>The release versioning script can preview the computed version without changing the repository.</summary>
    /// <intent>Support a safe check for what the next release would be before committing to any release action.</intent>
    /// <scenario>REPO-RELEASE-003</scenario>
    /// <behavior>When calculate-only mode is used, the script reports the computed bump and exits before validation or repo mutation.</behavior>
    [Fact]
    public async Task InvokeReleaseVersioning_CalculateOnlyReportsTargetVersion_WithoutMutatingFiles()
    {
        using TemporaryDirectory temp = new();
        await InitializeReleaseRepoAsync(temp.Path);

        await File.WriteAllTextAsync(
            Path.Combine(temp.Path, "src", "Test.Library", "PublicAPI.Shipped.txt"),
            """
            #nullable enable
            namespace Test.Library
            {
                public sealed class Example
                {
                    public void DoWork() { }
                }
            }
            """);

        string output = await InvokePowerShellAsync(
            temp.Path,
            """
            & ./scripts/release/Invoke-ReleaseVersioning.ps1 -CalculateOnly
            """);

        Assert.Contains("Required bump: Major", output, StringComparison.Ordinal);
        Assert.Contains("Target release version: 2.0.0", output, StringComparison.Ordinal);
        Assert.Contains("Calculation only; no files will be modified or release actions performed.", output, StringComparison.Ordinal);
        Assert.Contains("<Version>1.0.14</Version>", await File.ReadAllTextAsync(Path.Combine(temp.Path, "Directory.Build.props")), StringComparison.Ordinal);
    }

    private static async Task InitializeReleaseRepoAsync(string path)
    {
        Directory.CreateDirectory(Path.Combine(path, "scripts", "release"));
        Directory.CreateDirectory(Path.Combine(path, "src", "Test.Library"));

        await File.WriteAllTextAsync(
            Path.Combine(path, "Directory.Build.props"),
            """
            <Project>
              <PropertyGroup>
                <Version>1.0.14</Version>
              </PropertyGroup>
            </Project>
            """);

        await File.WriteAllTextAsync(
            Path.Combine(path, "src", "Test.Library", "Test.Library.csproj"),
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);

        await File.WriteAllTextAsync(
            Path.Combine(path, "src", "Test.Library", "PublicAPI.Shipped.txt"),
            """
            #nullable enable
            namespace Test.Library
            {
                public sealed class Example
                {
                    public Example() { }
                }
            }
            """);

        await File.WriteAllTextAsync(
            Path.Combine(path, "README.md"),
            "initial" + Environment.NewLine);

        string releaseScriptSource = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "scripts", "release", "Invoke-ReleaseVersioning.ps1"));
        await File.WriteAllTextAsync(
            Path.Combine(path, "scripts", "release", "Invoke-ReleaseVersioning.ps1"),
            await File.ReadAllTextAsync(releaseScriptSource));

        await File.WriteAllTextAsync(
            Path.Combine(path, "scripts", "release", "validate-public-api-versioning.ps1"),
            await File.ReadAllTextAsync(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "scripts", "release", "validate-public-api-versioning.ps1"))));

        await RunProcessAsync(path, "git", "init");
        await RunProcessAsync(path, "git", "config user.email \"chatkit-tests@example.com\"");
        await RunProcessAsync(path, "git", "config user.name \"ChatKit Tests\"");
        await RunProcessAsync(path, "git", "add -A");
        await RunProcessAsync(path, "git", "commit -m \"initial release\"");
        await RunProcessAsync(path, "git", "tag v1.0.14");

        await File.AppendAllTextAsync(Path.Combine(path, "README.md"), "dirty" + Environment.NewLine);
    }

    private static async Task<string> InvokePowerShellAsync(string workingDirectory, string script)
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
            WorkingDirectory = workingDirectory,
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

    private static async Task RunProcessAsync(string workingDirectory, string fileName, string arguments)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start {fileName}.");

        string stdout = await process.StandardOutput.ReadToEndAsync();
        string stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new Xunit.Sdk.XunitException($"{fileName} {arguments} exited with code {process.ExitCode}.{Environment.NewLine}STDOUT:{Environment.NewLine}{stdout}{Environment.NewLine}STDERR:{Environment.NewLine}{stderr}");
        }
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
                ResetAttributes(Path);

                for (int attempt = 0; attempt < 5; attempt++)
                {
                    try
                    {
                        Directory.Delete(Path, recursive: true);
                        return;
                    }
                    catch (UnauthorizedAccessException) when (attempt < 4)
                    {
                        global::System.Threading.Thread.Sleep(100);
                    }
                    catch (IOException) when (attempt < 4)
                    {
                        global::System.Threading.Thread.Sleep(100);
                    }
                }
            }
        }

        private static void ResetAttributes(string root)
        {
            foreach (string filePath in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(filePath, FileAttributes.Normal);
            }
        }
    }
}
