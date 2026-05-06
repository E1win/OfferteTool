using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface ITenderChangeLogRepository
{
    Task AddAsync(TenderChangeLog changeLog);
    Task<List<TenderChangeLog>> GetByTenderAsync(Guid tenderId);
    Task SaveChangesAsync();
}
