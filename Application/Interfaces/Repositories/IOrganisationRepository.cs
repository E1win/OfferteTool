using Domain.Entities;
using Domain.Enums;

namespace Application.Interfaces.Repositories;

public interface IOrganisationRepository
{
    Task<List<Organisation>> GetAllAsync(bool includeInactive = false);
    Task<List<Organisation>> GetByTypeAsync(OrganisationType organisationType, bool includeInactive = false);
    Task<Organisation?> GetByIdAsync(Guid organisationId);
    Task<Organisation?> GetByKvkNumberAsync(string kvkNumber);
    Task<bool> ExistsByKvkNumberAsync(string kvkNumber, Guid? exceptOrganisationId = null);
    Task<Organisation> AddAsync(Organisation organisation);
    Task SaveChangesAsync();
}
