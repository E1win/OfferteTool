using Domain.Entities;
using Domain.Entities.TenderAnswers;

namespace Application.Interfaces.Services;

public interface ITenderSubmissionService
{
    Task<TenderSubmission?> GetByTenderForCurrentSupplierAsync(Guid tenderId, string userId);
    Task<TenderSubmission> SubmitAsync(Guid tenderId, IEnumerable<TenderAnswer> answers, string userId);
}
