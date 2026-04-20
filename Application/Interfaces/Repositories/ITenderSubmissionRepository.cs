using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface ITenderSubmissionRepository
{
    Task<TenderSubmission?> GetByTenderAndSupplierAsync(Guid tenderId, Guid supplierId);
    Task<TenderSubmission> AddAsync(TenderSubmission submission);
    Task SaveChangesAsync();
}
