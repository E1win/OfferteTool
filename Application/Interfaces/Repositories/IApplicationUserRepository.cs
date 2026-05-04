using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface IApplicationUserRepository
{
    Task<List<ApplicationUser>> GetAllAsync();
    Task<ApplicationUser?> GetByIdAsync(string userId);
    Task<List<ApplicationUser>> GetByOrganisationAsync(Guid organisationId);
    Task<List<ApplicationUser>> GetByIdsAsync(IEnumerable<string> userIds);
    Task<IReadOnlyList<string>> GetRolesAsync(ApplicationUser user);
}
