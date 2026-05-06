using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class TenderChangeLogRepository(AppDbContext dbContext) : ITenderChangeLogRepository
{
    public async Task AddAsync(TenderChangeLog changeLog)
    {
        await dbContext.TenderChangeLogs.AddAsync(changeLog);
    }

    public async Task<List<TenderChangeLog>> GetByTenderAsync(Guid tenderId) =>
        await dbContext.TenderChangeLogs
            .Where(changeLog => changeLog.TenderId == tenderId)
            .OrderByDescending(changeLog => changeLog.ChangedAtUtc)
            .ToListAsync();

    public async Task SaveChangesAsync()
    {
        await dbContext.SaveChangesAsync();
    }
}
