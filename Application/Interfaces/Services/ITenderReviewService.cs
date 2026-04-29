using Application.Models.TenderReview;
using Domain.Entities;

namespace Application.Interfaces.Services;

public interface ITenderReviewService
{
    Task<TenderSubmission> GetSubmissionForReviewAsync(Guid tenderId, Guid submissionId, string reviewerUserId);
    Task<TenderSubmissionReview?> GetReviewAsync(Guid tenderId, Guid submissionId, string reviewerUserId);
    Task<List<TenderSubmissionReview>> GetReviewsAsync(Guid tenderId, Guid submissionId, string reviewerUserId);
    Task<TenderSubmissionReview> SaveReviewAsync(
        Guid tenderId,
        Guid submissionId,
        IEnumerable<TenderQuestionRatingInput> ratings,
        string reviewerUserId);
}
