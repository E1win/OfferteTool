using Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Domain.Entities;

public class ApplicationUser : IdentityUser
{
    private static readonly Dictionary<string, OrganisationType> AllowedOrganisationTypes = new()
    {
        { "Inkoper", OrganisationType.Client },
        { "Beoordelaar", OrganisationType.Client },
        { "Leverancier", OrganisationType.Supplier },
    };

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    public Guid? OrganisationId { get; set; }
    public Organisation? Organisation { get; set; }

    public bool RequiresOrganisation(string role) => AllowedOrganisationTypes.ContainsKey(role);

    public bool CanAttachToOrganisation(string role, OrganisationType organisationType)
    {
        if (!AllowedOrganisationTypes.TryGetValue(role, out var allowedType))
            return false;

        return allowedType == organisationType;
    }
}
