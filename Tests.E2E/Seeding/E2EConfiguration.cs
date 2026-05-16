using System.Text.Json;

namespace Tests.E2E.Seeding;

public sealed class E2EConfiguration
{
    private E2EConfiguration(string connectionString, string encryptionAlgorithm, string encryptionKey)
    {
        ConnectionString = connectionString;
        EncryptionAlgorithm = encryptionAlgorithm;
        EncryptionKey = encryptionKey;
    }

    public string ConnectionString { get; }
    public string EncryptionAlgorithm { get; }
    public string EncryptionKey { get; }

    public static E2EConfiguration Load()
    {
        var root = FindSolutionRoot();
        var path = Path.Combine(root, "Presentation", "appsettings.E2E.json");

        if (!File.Exists(path))
            throw new FileNotFoundException("Kan Presentation/appsettings.E2E.json niet vinden.", path);

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var connectionString = document.RootElement
            .GetProperty("ConnectionStrings")
            .GetProperty("DefaultConnection")
            .GetString();
        var encryptionKey = document.RootElement
            .GetProperty("TenderSubmissionEncryption")
            .GetProperty("Key")
            .GetString();
        var encryptionAlgorithm = document.RootElement
            .GetProperty("TenderSubmissionEncryption")
            .GetProperty("Algorithm")
            .GetString();

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection ontbreekt in Presentation/appsettings.E2E.json.");

        if (string.IsNullOrWhiteSpace(encryptionAlgorithm))
            throw new InvalidOperationException("TenderSubmissionEncryption:Algorithm ontbreekt in Presentation/appsettings.E2E.json.");

        if (string.IsNullOrWhiteSpace(encryptionKey))
            throw new InvalidOperationException("TenderSubmissionEncryption:Key ontbreekt in Presentation/appsettings.E2E.json.");

        return new E2EConfiguration(connectionString, encryptionAlgorithm, encryptionKey);
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
}
