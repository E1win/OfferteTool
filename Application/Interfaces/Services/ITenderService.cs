using Domain.Entities;

namespace Application.Interfaces.Services;

public interface ITenderService
{
    Task<List<Tender>> GetAccessibleTendersAsync(string userId);
}
