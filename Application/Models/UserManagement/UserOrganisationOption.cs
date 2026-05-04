using Domain.Enums;

namespace Application.Models.UserManagement;

public class UserOrganisationOption
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required OrganisationType OrganisationType { get; set; }
}
