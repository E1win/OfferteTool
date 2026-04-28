using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface ITenderSubmissionReviewRepository
{
    Task<TenderSubmission?> GetSubmissionForReviewAsync(Guid submissionId);
    Task<TenderSubmissionReview?> GetBySubmissionAndReviewerAsync(Guid submissionId, string reviewerUserId);
    Task<List<TenderSubmissionReview>> GetBySubmissionAsync(Guid submissionId);
    Task<TenderSubmissionReview> AddAsync(TenderSubmissionReview review);
    Task SaveChangesAsync();
}
