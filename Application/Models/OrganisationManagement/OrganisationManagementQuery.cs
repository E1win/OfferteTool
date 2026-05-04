using Domain.Enums;

namespace Application.Models.OrganisationManagement;

public class OrganisationManagementQuery
{
    public string? Search { get; set; }
    public OrganisationType? OrganisationType { get; set; }
    public bool IncludeInactive { get; set; }
}
