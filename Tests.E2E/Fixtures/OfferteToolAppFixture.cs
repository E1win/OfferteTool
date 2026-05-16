using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Sockets;

namespace Tests.E2E.Fixtures;

public sealed class OfferteToolAppFixture : IAsyncLifetime
{
    public const string BaseUrl = "https://localhost:7018";
    private const string Host = "localhost";
    private const int Port = 7018;

    private readonly ConcurrentQueue<string> output = new();
    private Process? process;

    public async Task InitializeAsync()
    {
        var root = FindSolutionRoot();

        if (await IsReadyAsync())
        {
            throw new InvalidOperationException(
                $"{BaseUrl} is al in gebruik. Stop de handmatig gestarte E2E app, zodat de fixture de database kan resetten en de app zelf met de E2E environment kan starten.");
        }

        var presentationProject = Path.Combine(root, "Presentation", "Presentation.csproj");

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(presentationProject);
        startInfo.ArgumentList.Add("--launch-profile");
        startInfo.ArgumentList.Add("e2e");
        startInfo.ArgumentList.Add("--no-build");
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "E2E";
        startInfo.Environment["DOTNET_ENVIRONMENT"] = "E2E";

        process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Kon de E2E app niet starten.");

        process.OutputDataReceived += (_, args) => AddOutput(args.Data);
        process.ErrorDataReceived += (_, args) => AddOutput(args.Data);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            await WaitUntilReadyAsync();
        }
        catch
        {
            await StopProcessAsync();
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        if (process is null)
            return;

        await StopProcessAsync();
    }

    private async Task WaitUntilReadyAsync()
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(90);

        while (DateTimeOffset.UtcNow < deadline)
        {
            ThrowIfProcessExited();

            if (await IsReadyAsync())
                return;

            await Task.Delay(500);
        }

        throw new TimeoutException($"De E2E app werd niet bereikbaar op {BaseUrl}.{Environment.NewLine}{GetRecentOutput()}");
    }

    private static async Task<bool> IsReadyAsync()
    {
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            using var client = new TcpClient();

            await client.ConnectAsync(Host, Port, timeout.Token);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    private void ThrowIfProcessExited()
    {
        if (process is null || !process.HasExited)
            return;

        throw new InvalidOperationException(
            $"De E2E app stopte voordat hij bereikbaar werd. Exit code: {process.ExitCode}.{Environment.NewLine}{GetRecentOutput()}");
    }

    private async Task StopProcessAsync()
    {
        if (process is null)
            return;

        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }
        }
        finally
        {
            process.Dispose();
            process = null;
        }
    }

    private static string FindSolutionRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Presentation", "Presentation.csproj")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Kon de solution root met Presentation/Presentation.csproj niet vinden.");
    }

    private void AddOutput(string? line)
    {
        if (!string.IsNullOrWhiteSpace(line))
            output.Enqueue(line);
    }

    private string GetRecentOutput()
    {
        var lines = output.TakeLast(80).ToArray();

        return lines.Length == 0
            ? "Er was geen procesoutput beschikbaar."
            : string.Join(Environment.NewLine, lines);
    }
}
