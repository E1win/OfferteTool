using Presentation.Models.OrganisationManagement;

namespace Presentation.Builders;

public interface IOrganisationManagementPageModelBuilder
{
    Task<OrganisationManagementIndexViewModel> BuildIndexAsync(
        string? search = null,
        bool includeInactive = false,
        OrganisationFormViewModel? createOrganisation = null,
        bool openCreateOrganisationModal = false,
        string? createErrorMessage = null,
        OrganisationFormViewModel? editOrganisation = null,
        bool openEditOrganisationModal = false,
        string? editErrorMessage = null);
}
