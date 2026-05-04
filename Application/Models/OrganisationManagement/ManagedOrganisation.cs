using Domain.Enums;

namespace Application.Models.OrganisationManagement;

public class ManagedOrganisation
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string KvkNumber { get; set; } = string.Empty;
    public OrganisationType OrganisationType { get; set; }
    public bool IsActive { get; set; }
}
