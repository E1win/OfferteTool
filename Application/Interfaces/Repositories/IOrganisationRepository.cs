using Domain.Entities;
using Domain.Enums;

namespace Application.Interfaces.Repositories;

public interface IOrganisationRepository
{
    Task<List<Organisation>> GetAllAsync();
    Task<List<Organisation>> GetByTypeAsync(OrganisationType organisationType);
    Task<Organisation?> GetByIdAsync(Guid organisationId);
}
