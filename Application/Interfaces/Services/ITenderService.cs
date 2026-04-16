using Domain.Entities;

namespace Application.Interfaces.Services;

public interface ITenderService
{
    Task<List<Tender>> GetAccessibleTendersAsync(string userId);
    Task<Tender> GetAccessibleTenderByIdAsync(Guid tenderId, string userId);
    Task<bool> CanManageTenderAsync(Guid tenderId, string userId);
    Task<Tender> CreateTenderAsync(Tender tender, string userId);
    Task<Tender> UpdateTenderAsync(Guid tenderId, Tender updatedTender, string userId);
    Task<Tender> OpenTenderAsync(Guid tenderId, string userId);
}
