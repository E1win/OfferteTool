using Domain.Entities;

namespace Application.Interfaces.Services;

public interface ITenderSubmissionEncryptionService
{
    void Encrypt(TenderSubmission submission);
    void Decrypt(TenderSubmission submission);
}
