using Application.Models.OrganisationManagement;

namespace Presentation.Models.OrganisationManagement;

public class OrganisationManagementIndexViewModel
{
    public string Search { get; set; } = string.Empty;
    public bool IncludeInactive { get; set; }
    public List<ManagedOrganisation> Organisations { get; set; } = [];
    public OrganisationFormModalViewModel CreateOrganisationModal { get; set; } = new();
    public OrganisationFormModalViewModel EditOrganisationModal { get; set; } = new();
}
