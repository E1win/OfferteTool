namespace Infrastructure.Configuration;

public class TenderSubmissionEncryptionOptions
{
    public const string SectionName = "TenderSubmissionEncryption";

    public string Algorithm { get; set; } = "AES-256-GCM";
    public string Key { get; set; } = string.Empty;
}
