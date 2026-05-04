using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class OrganisationRepository(AppDbContext dbContext) : IOrganisationRepository
{
    public async Task<List<Organisation>> GetAllAsync() =>
        await dbContext.Organisations
            .OrderBy(organisation => organisation.Name)
            .ThenBy(organisation => organisation.KvkNumber)
            .ToListAsync();

    public async Task<List<Organisation>> GetByTypeAsync(OrganisationType organisationType) =>
        await dbContext.Organisations
            .Where(organisation => organisation.OrganisationType == organisationType)
            .OrderBy(organisation => organisation.Name)
            .ThenBy(organisation => organisation.KvkNumber)
            .ToListAsync();

    public async Task<Organisation?> GetByIdAsync(Guid organisationId) =>
        await dbContext.Organisations.FindAsync(organisationId);
}
