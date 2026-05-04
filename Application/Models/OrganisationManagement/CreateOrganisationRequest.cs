using Domain.Enums;

namespace Application.Models.OrganisationManagement;

public class CreateOrganisationRequest
{
    public string Name { get; set; } = string.Empty;
    public string KvkNumber { get; set; } = string.Empty;
    public OrganisationType OrganisationType { get; set; }
}
