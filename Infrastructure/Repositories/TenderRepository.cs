using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Entities.TenderQuestions;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Infrastructure.Repositories;

public class TenderRepository(AppDbContext dbContext) : ITenderRepository
{
    public async Task<Tender?> GetByIdAsync(Guid id) =>
        await dbContext.Tenders
            .Include(t => t.Organisation)
            .FirstOrDefaultAsync(t => t.Id == id);

    public async Task<Tender?> GetByIdWithReviewersAsync(Guid id) =>
        await dbContext.Tenders
            .Include(t => t.Organisation)
            .Include(t => t.Reviewers)
            .FirstOrDefaultAsync(t => t.Id == id);

    public async Task<Tender?> GetByIdWithQuestionsAndOptionsAsync(Guid id) =>
        await dbContext.Tenders
            .Include(t => t.Organisation)
            .Include(t => t.Questions)
            .ThenInclude(q => ((ChoiceQuestion)q).Options)
            .FirstOrDefaultAsync(t => t.Id == id);

    public async Task<Tender?> GetByIdWithComparisonDataAsync(Guid id) =>
        await dbContext.Tenders
            .Include(t => t.Questions)
            .Include(t => t.Reviewers)
            .Include(t => t.Submissions)
            .ThenInclude(submission => submission.Supplier)
            .Include(t => t.Submissions)
            .ThenInclude(submission => submission.Reviews)
            .ThenInclude(review => review.QuestionReviews)
            .FirstOrDefaultAsync(t => t.Id == id);

    public async Task<List<Tender>> GetByOrganisationAsync(Guid organisationId) =>
        await dbContext.Tenders
            .Where(t => t.OrganisationId == organisationId)
            .ToListAsync();

    public async Task<List<Tender>> GetClosedByReviewerAsync(string reviewerUserId) =>
        await dbContext.Tenders
            .Include(t => t.Organisation)
            .Include(t => t.Reviewers)
            .Where(t => t.Status == TenderStatus.Closed && t.Reviewers.Any(reviewer => reviewer.UserId == reviewerUserId))
            .ToListAsync();

    public async Task<List<Tender>> GetPublicOpenAsync() =>
        await dbContext.Tenders
            .Where(t => t.IsPublic && t.Status == TenderStatus.Open)
            .ToListAsync();

    public async Task<Tender> AddAsync(Tender tender)
    {
        await dbContext.Tenders.AddAsync(tender);
        await dbContext.SaveChangesAsync();
        return tender;
    }

    public async Task UpdateAsync()
    {
        await dbContext.SaveChangesAsync();
    }
}
