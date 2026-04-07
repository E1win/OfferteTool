using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class TenderRepository(AppDbContext dbContext) : ITenderRepository
{
    public async Task<Tender?> GetByIdAsync(Guid id) =>
        await dbContext.Tenders.FindAsync(id);

    public async Task<List<Tender>> GetByOrganisationAsync(Guid organisationId) =>
        await dbContext.Tenders
            .Where(t => t.OrganisationId == organisationId)
            .ToListAsync();

    public async Task<List<Tender>> GetPublicOpenAsync() =>
        await dbContext.Tenders
            .Where(t => t.IsPublic && t.Status == TenderStatus.Open)
            .ToListAsync();
}
