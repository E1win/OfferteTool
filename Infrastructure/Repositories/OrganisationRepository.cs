using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class OrganisationRepository(AppDbContext dbContext) : IOrganisationRepository
{
    public async Task<List<Organisation>> GetAllAsync(bool includeInactive = false) =>
        await ApplyActiveFilter(dbContext.Organisations, includeInactive)
            .OrderBy(organisation => organisation.Name)
            .ThenBy(organisation => organisation.KvkNumber)
            .ToListAsync();

    public async Task<List<Organisation>> GetByTypeAsync(OrganisationType organisationType, bool includeInactive = false) =>
        await ApplyActiveFilter(dbContext.Organisations, includeInactive)
            .Where(organisation => organisation.OrganisationType == organisationType)
            .OrderBy(organisation => organisation.Name)
            .ThenBy(organisation => organisation.KvkNumber)
            .ToListAsync();

    public async Task<Organisation?> GetByIdAsync(Guid organisationId) =>
        await dbContext.Organisations.FindAsync(organisationId);

    public async Task<Organisation?> GetByKvkNumberAsync(string kvkNumber) =>
        await dbContext.Organisations
            .FirstOrDefaultAsync(organisation => organisation.KvkNumber == kvkNumber);

    public async Task<bool> ExistsByKvkNumberAsync(string kvkNumber, Guid? exceptOrganisationId = null) =>
        await dbContext.Organisations
            .AnyAsync(organisation =>
                organisation.KvkNumber == kvkNumber
                && (exceptOrganisationId == null || organisation.Id != exceptOrganisationId.Value));

    public async Task<Organisation> AddAsync(Organisation organisation)
    {
        await dbContext.Organisations.AddAsync(organisation);
        await dbContext.SaveChangesAsync();
        return organisation;
    }

    public async Task SaveChangesAsync()
    {
        await dbContext.SaveChangesAsync();
    }

    private static IQueryable<Organisation> ApplyActiveFilter(
        IQueryable<Organisation> organisations,
        bool includeInactive) =>
        includeInactive
            ? organisations
            : organisations.Where(organisation => organisation.IsActive);
}
