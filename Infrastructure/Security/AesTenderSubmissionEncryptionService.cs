using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Entities.TenderAnswers;
using Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Security;

public class AesTenderSubmissionEncryptionService(
    IOptions<TenderSubmissionEncryptionOptions> options,
    TenderAnswerPayloadSerializer payloadSerializer) : ITenderSubmissionEncryptionService
{
    private const int KeySizeInBytes = 32;
    private const int NonceSizeInBytes = 12;
    private const int TagSizeInBytes = 16;

    private readonly TenderSubmissionEncryptionOptions encryptionOptions = options.Value;

    public void Encrypt(TenderSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(submission);

        var key = GetKeyBytes();

        foreach (var answer in submission.Answers)
        {
            var payload = payloadSerializer.Serialize(answer);
            // AES-GCM requires a unique nonce per encryption with the same key.
            var nonce = RandomNumberGenerator.GetBytes(NonceSizeInBytes);
            var ciphertext = new byte[payload.Length];
            var tag = new byte[TagSizeInBytes];
            var additionalAuthenticatedData = BuildAdditionalAuthenticatedData(submission, answer);

            using var aesGcm = new AesGcm(key, TagSizeInBytes);
            aesGcm.Encrypt(nonce, payload, ciphertext, tag, additionalAuthenticatedData);

            answer.EncryptedPayload = ciphertext;
            answer.Nonce = nonce;
            answer.Tag = tag;
        }
    }

    public void Decrypt(TenderSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(submission);

        if (submission.Answers.Count == 0)
            return;

        var key = GetKeyBytes();

        foreach (var answer in submission.Answers)
        {
            EnsureEncryptedAnswerIsComplete(answer);

            var payload = new byte[answer.EncryptedPayload.Length];
            // The same AAD must be supplied during decrypt, or tag validation fails.
            var additionalAuthenticatedData = BuildAdditionalAuthenticatedData(submission, answer);

            using var aesGcm = new AesGcm(key, TagSizeInBytes);
            aesGcm.Decrypt(answer.Nonce, answer.EncryptedPayload, answer.Tag, payload, additionalAuthenticatedData);

            payloadSerializer.Populate(answer, payload);
        }
    }

    private byte[] GetKeyBytes()
    {
        if (!string.Equals(encryptionOptions.Algorithm, "AES-256-GCM", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Alleen AES-256-GCM wordt ondersteund voor tenderinzendingen.");

        if (string.IsNullOrWhiteSpace(encryptionOptions.Key))
            throw new InvalidOperationException("Er is geen encryptiesleutel geconfigureerd voor tenderinzendingen.");

        try
        {
            var key = Convert.FromBase64String(encryptionOptions.Key);

            if (key.Length != KeySizeInBytes)
                throw new InvalidOperationException("De geconfigureerde encryptiesleutel moet exact 32 bytes zijn.");

            return key;
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException("De encryptiesleutel bevat geen geldige Base64-waarde.", ex);
        }
    }

    private static byte[] BuildAdditionalAuthenticatedData(TenderSubmission submission, TenderAnswer answer)
    {
        var aad = $"{submission.TenderId}|{submission.SupplierId}|{answer.QuestionId}|{answer.Type}";

        return Encoding.UTF8.GetBytes(aad);
    }

    private static void EnsureEncryptedAnswerIsComplete(TenderAnswer answer)
    {
        if (answer.EncryptedPayload.Length == 0)
            throw new InvalidOperationException("Een opgeslagen antwoord bevat geen versleutelde payload.");

        if (answer.Nonce.Length != NonceSizeInBytes)
            throw new InvalidOperationException("Een opgeslagen antwoord bevat geen geldige nonce.");

        if (answer.Tag.Length != TagSizeInBytes)
            throw new InvalidOperationException("Een opgeslagen antwoord bevat geen geldige authenticatietag.");
    }
}
