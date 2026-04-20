using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Entities.TenderAnswers;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class TenderSubmissionRepository(AppDbContext dbContext) : ITenderSubmissionRepository
{
    public async Task<TenderSubmission?> GetByTenderAndSupplierAsync(Guid tenderId, Guid supplierId) =>
        await dbContext.TenderSubmissions
            .Include(submission => submission.Answers)
            .ThenInclude(answer => ((ChoiceAnswer)answer).Selections)
            .FirstOrDefaultAsync(submission => submission.TenderId == tenderId && submission.SupplierId == supplierId);

    public async Task<TenderSubmission> AddAsync(TenderSubmission submission)
    {
        await dbContext.TenderSubmissions.AddAsync(submission);
        await dbContext.SaveChangesAsync();
        return submission;
    }

    public async Task SaveChangesAsync()
    {
        await dbContext.SaveChangesAsync();
    }
}
