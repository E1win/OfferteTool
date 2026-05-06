using Domain.Entities;

namespace Application.Interfaces.Services;

public interface ITenderChangeLogService
{
    Task<List<TenderChangeLog>> GetVisibleTenderChangesAsync(Guid tenderId, string userId);
}
