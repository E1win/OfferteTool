namespace Tests.E2E.Configuration;

public static class E2ERuntimeEnvironment
{
    public const string LocalFileName = ".env.e2e.local";
    public const string ExampleFileName = ".env.e2e.example";
    public const string DefaultConnectionKey = "ConnectionStrings__DefaultConnection";

    private static readonly object SyncRoot = new();
    private static bool isLoaded;

    public static void Load()
    {
        lock (SyncRoot)
        {
            if (isLoaded)
                return;

            var repositoryRoot = FindRepositoryRoot();
            var localEnvFile = Path.Combine(repositoryRoot, LocalFileName);

            if (File.Exists(localEnvFile))
            {
                LoadFile(localEnvFile);
            }

            isLoaded = true;
        }
    }

    public static string GetRepositoryRoot() => FindRepositoryRoot();

    private static void LoadFile(string path)
    {
        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();

            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            var separatorIndex = line.IndexOf('=');

            if (separatorIndex <= 0)
                continue;

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();

            if (value.Length >= 2 && IsQuoted(value))
                value = value[1..^1];

            if (Environment.GetEnvironmentVariable(key) is null)
                Environment.SetEnvironmentVariable(key, value);
        }
    }

    private static bool IsQuoted(string value) =>
        (value[0] == '"' && value[^1] == '"')
        || (value[0] == '\'' && value[^1] == '\'');

    private static string FindRepositoryRoot()
    {
        var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);

        while (currentDirectory is not null)
        {
            if (File.Exists(Path.Combine(currentDirectory.FullName, "OfferteTool.slnx")))
                return currentDirectory.FullName;

            currentDirectory = currentDirectory.Parent;
        }

        return Directory.GetCurrentDirectory();
    }
}
