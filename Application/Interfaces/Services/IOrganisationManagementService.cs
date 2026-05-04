using Application.Models.OrganisationManagement;

namespace Application.Interfaces.Services;

public interface IOrganisationManagementService
{
    Task<List<ManagedOrganisation>> GetOrganisationsAsync(OrganisationManagementQuery query);
    Task<ManagedOrganisation> GetOrganisationAsync(Guid organisationId);
    Task<ManagedOrganisation> CreateOrganisationAsync(CreateOrganisationRequest request, string actorUserId);
    Task<ManagedOrganisation> UpdateOrganisationAsync(UpdateOrganisationRequest request, string actorUserId);
    Task DeactivateOrganisationAsync(Guid organisationId, string actorUserId);
}
