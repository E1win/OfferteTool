using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface ITenderRepository
{
    Task<Tender?> GetByIdAsync(Guid id);
    Task<Tender?> GetByIdWithQuestionsAndOptionsAsync(Guid id);
    Task<List<Tender>> GetByOrganisationAsync(Guid organisationId);
    Task<List<Tender>> GetPublicOpenAsync();
    Task<Tender> AddAsync(Tender tender);
    Task UpdateAsync();
}
