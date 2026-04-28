using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Entities.TenderQuestions;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class TenderSubmissionReviewRepository(AppDbContext dbContext) : ITenderSubmissionReviewRepository
{
    public async Task<TenderSubmission?> GetSubmissionForReviewAsync(Guid submissionId) =>
        await dbContext.TenderSubmissions
            .Include(submission => submission.Supplier)
            .Include(submission => submission.Answers)
            .Include(submission => submission.Tender)
            .ThenInclude(tender => tender!.Reviewers)
            .Include(submission => submission.Tender)
            .ThenInclude(tender => tender!.Questions)
            .ThenInclude(question => ((ChoiceQuestion)question).Options)
            .FirstOrDefaultAsync(submission => submission.Id == submissionId);

    public async Task<TenderSubmissionReview?> GetBySubmissionAndReviewerAsync(Guid submissionId, string reviewerUserId) =>
        await dbContext.TenderSubmissionReviews
            .Include(review => review.QuestionReviews)
            .FirstOrDefaultAsync(review => review.SubmissionId == submissionId && review.ReviewerUserId == reviewerUserId);

    public async Task<List<TenderSubmissionReview>> GetBySubmissionAsync(Guid submissionId) =>
        await dbContext.TenderSubmissionReviews
            .Include(review => review.Reviewer)
            .Include(review => review.QuestionReviews)
            .Where(review => review.SubmissionId == submissionId)
            .ToListAsync();

    public async Task<TenderSubmissionReview> AddAsync(TenderSubmissionReview review)
    {
        await dbContext.TenderSubmissionReviews.AddAsync(review);
        await dbContext.SaveChangesAsync();
        return review;
    }

    public async Task SaveChangesAsync()
    {
        await dbContext.SaveChangesAsync();
    }
}
