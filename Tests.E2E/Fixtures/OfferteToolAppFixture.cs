using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Tests.E2E.Configuration;

namespace Tests.E2E.Fixtures;

public sealed class OfferteToolAppFixture : IAsyncLifetime
{
    private readonly DatabaseFixture database = new();
    private readonly List<string> output = [];
    private readonly object outputLock = new();
    private Process? process;

    public string BaseUrl { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        E2ERuntimeEnvironment.Load();

        await database.InitializeAsync();

        var port = GetAvailablePort();
        BaseUrl = $"http://127.0.0.1:{port}";

        var repositoryRoot = E2ERuntimeEnvironment.GetRepositoryRoot();
        var presentationProjectPath = Path.Combine(repositoryRoot, "Presentation", "Presentation.csproj");

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --no-launch-profile --no-build --no-restore --project \"{presentationProjectPath}\" --urls \"{BaseUrl}\"",
            WorkingDirectory = repositoryRoot,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "E2E";
        startInfo.Environment[E2ERuntimeEnvironment.DefaultConnectionKey] = database.ConnectionString;

        process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start the Presentation app process.");

        _ = Task.Run(() => DrainOutputAsync(process.StandardOutput));
        _ = Task.Run(() => DrainOutputAsync(process.StandardError));

        await WaitUntilReadyAsync();
    }

    public async Task DisposeAsync()
    {
        if (process is not null && !process.HasExited)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
        }

        await database.DisposeAsync();
    }

    private async Task WaitUntilReadyAsync()
    {
        using var httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(2)
        };

        var deadline = DateTimeOffset.UtcNow.AddSeconds(60);
        Exception? lastException = null;

        while (DateTimeOffset.UtcNow < deadline)
        {
            ThrowIfProcessExited();

            try
            {
                using var response = await httpClient.GetAsync(BaseUrl);

                if ((int)response.StatusCode < 500)
                    return;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                lastException = ex;
            }

            await Task.Delay(250);
        }

        throw new TimeoutException($"The app did not become ready at {BaseUrl}.{Environment.NewLine}{GetOutput()}", lastException);
    }

    private void ThrowIfProcessExited()
    {
        if (process is not null && process.HasExited)
        {
            throw new InvalidOperationException(
                $"The app process exited before it became ready. Exit code: {process.ExitCode}.{Environment.NewLine}{GetOutput()}");
        }
    }

    private static int GetAvailablePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        try
        {
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }

    private async Task DrainOutputAsync(StreamReader reader)
    {
        while (await reader.ReadLineAsync() is { } line)
        {
            lock (outputLock)
            {
                output.Add(line);
            }
        }
    }

    private string GetOutput()
    {
        lock (outputLock)
        {
            return string.Join(Environment.NewLine, output.TakeLast(80));
        }
    }
}
