using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class TenderSubmissionRepository(AppDbContext dbContext) : ITenderSubmissionRepository
{
    public async Task<TenderSubmission?> GetByTenderAndSupplierAsync(Guid tenderId, Guid supplierId) =>
        await dbContext.TenderSubmissions
            .Include(submission => submission.Answers)
            .FirstOrDefaultAsync(submission => submission.TenderId == tenderId && submission.SupplierId == supplierId);

    public async Task<List<TenderSubmission>> GetByTenderWithSuppliersAsync(Guid tenderId) =>
        await dbContext.TenderSubmissions
            .Include(submission => submission.Supplier)
            .Where(submission => submission.TenderId == tenderId)
            .OrderBy(submission => submission.Supplier!.Name)
            .ToListAsync();

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
