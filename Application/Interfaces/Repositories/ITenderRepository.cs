using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface ITenderRepository
{
    Task<Tender?> GetByIdAsync(Guid id);
    Task<Tender?> GetByIdWithReviewersAsync(Guid id);
    Task<Tender?> GetByIdWithQuestionsAndOptionsAsync(Guid id);
    Task<Tender?> GetByIdWithComparisonDataAsync(Guid id);
    Task<List<Tender>> GetByOrganisationAsync(Guid organisationId);
    Task<List<Tender>> GetClosedByReviewerAsync(string reviewerUserId);
    Task<List<Tender>> GetPublicOpenAsync();
    Task<Tender> AddAsync(Tender tender);
    Task UpdateAsync();
}
