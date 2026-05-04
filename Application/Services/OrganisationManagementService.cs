using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Models.OrganisationManagement;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Microsoft.AspNetCore.Identity;

namespace Application.Services;

public class OrganisationManagementService(
    IApplicationUserRepository applicationUserRepository,
    IOrganisationRepository organisationRepository,
    UserManager<ApplicationUser> userManager) : IOrganisationManagementService
{
    public async Task<List<ManagedOrganisation>> GetOrganisationsAsync(OrganisationManagementQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        var organisations = query.OrganisationType is null
            ? await organisationRepository.GetAllAsync(query.IncludeInactive)
            : await organisationRepository.GetByTypeAsync(query.OrganisationType.Value, query.IncludeInactive);

        return ApplySearch(organisations, query.Search)
            .Select(MapToManagedOrganisation)
            .ToList();
    }

    public async Task<ManagedOrganisation> GetOrganisationAsync(Guid organisationId)
    {
        var organisation = await organisationRepository.GetByIdAsync(organisationId)
            ?? throw new BusinessRuleViolationException("De organisatie bestaat niet.");

        return MapToManagedOrganisation(organisation);
    }

    public async Task<ManagedOrganisation> CreateOrganisationAsync(CreateOrganisationRequest request, string actorUserId)
    {
        await EnsureActorIsBeheerderAsync(actorUserId);
        ArgumentNullException.ThrowIfNull(request);

        var kvkNumber = NormalizeKvkNumber(request.KvkNumber);
        await EnsureKvkNumberIsUniqueAsync(kvkNumber);

        var organisation = new Organisation
        {
            Id = Guid.NewGuid(),
            Name = NormalizeName(request.Name),
            KvkNumber = kvkNumber,
            OrganisationType = request.OrganisationType,
            IsActive = true
        };

        await organisationRepository.AddAsync(organisation);

        return MapToManagedOrganisation(organisation);
    }

    public async Task<ManagedOrganisation> UpdateOrganisationAsync(UpdateOrganisationRequest request, string actorUserId)
    {
        await EnsureActorIsBeheerderAsync(actorUserId);
        ArgumentNullException.ThrowIfNull(request);

        var organisation = await organisationRepository.GetByIdAsync(request.Id)
            ?? throw new BusinessRuleViolationException("De organisatie bestaat niet.");
        var kvkNumber = NormalizeKvkNumber(request.KvkNumber);

        await EnsureKvkNumberIsUniqueAsync(kvkNumber, organisation.Id);

        organisation.Name = NormalizeName(request.Name);
        organisation.KvkNumber = kvkNumber;
        organisation.OrganisationType = request.OrganisationType;

        if (!request.IsActive)
            await DeactivateOrganisationAndUsersAsync(organisation);
        else
            organisation.IsActive = true;

        await organisationRepository.SaveChangesAsync();

        return MapToManagedOrganisation(organisation);
    }

    public async Task DeactivateOrganisationAsync(Guid organisationId, string actorUserId)
    {
        await EnsureActorIsBeheerderAsync(actorUserId);

        var organisation = await organisationRepository.GetByIdAsync(organisationId)
            ?? throw new BusinessRuleViolationException("De organisatie bestaat niet.");

        await DeactivateOrganisationAndUsersAsync(organisation);
        await organisationRepository.SaveChangesAsync();
    }

    private async Task DeactivateOrganisationAndUsersAsync(Organisation organisation)
    {
        organisation.IsActive = false;

        var linkedUsers = await applicationUserRepository.GetByOrganisationAsync(organisation.Id);

        foreach (var user in linkedUsers)
            user.IsActive = false;
    }

    private async Task EnsureActorIsBeheerderAsync(string actorUserId)
    {
        var actor = await applicationUserRepository.GetByIdAsync(actorUserId)
            ?? throw new UnauthorizedAccessException("U bent niet gemachtigd om organisaties te beheren.");
        var actorRoles = await userManager.GetRolesAsync(actor);

        if (!actor.IsActive || !actorRoles.Contains(Roles.Beheerder))
            throw new UnauthorizedAccessException("U bent niet gemachtigd om organisaties te beheren.");
    }

    private async Task EnsureKvkNumberIsUniqueAsync(string kvkNumber, Guid? exceptOrganisationId = null)
    {
        if (await organisationRepository.ExistsByKvkNumberAsync(kvkNumber, exceptOrganisationId))
            throw new BusinessRuleViolationException("Er bestaat al een organisatie met dit KvK-nummer.");
    }

    private static ManagedOrganisation MapToManagedOrganisation(Organisation organisation) =>
        new()
        {
            Id = organisation.Id,
            Name = organisation.Name,
            KvkNumber = organisation.KvkNumber,
            OrganisationType = organisation.OrganisationType,
            IsActive = organisation.IsActive
        };

    private static List<Organisation> ApplySearch(List<Organisation> organisations, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return organisations;

        var normalizedSearch = search.Trim();

        return organisations
            .Where(organisation =>
                Contains(organisation.Name, normalizedSearch)
                || Contains(organisation.KvkNumber, normalizedSearch)
                || Contains(FormatOrganisationType(organisation.OrganisationType), normalizedSearch))
            .ToList();
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessRuleViolationException("Vul een organisatienaam in.");

        var normalizedName = name.Trim();

        if (normalizedName.Length > 256)
            throw new BusinessRuleViolationException("De organisatienaam mag maximaal 256 tekens bevatten.");

        return normalizedName;
    }

    private static string NormalizeKvkNumber(string kvkNumber)
    {
        if (string.IsNullOrWhiteSpace(kvkNumber))
            throw new BusinessRuleViolationException("Vul een KvK-nummer in.");

        var normalizedKvkNumber = kvkNumber.Trim();

        if (normalizedKvkNumber.Length != 8 || normalizedKvkNumber.Any(character => !char.IsDigit(character)))
            throw new BusinessRuleViolationException("Het KvK-nummer moet uit 8 cijfers bestaan.");

        return normalizedKvkNumber;
    }

    private static string FormatOrganisationType(OrganisationType organisationType) =>
        organisationType switch
        {
            OrganisationType.Client => "Opdrachtgever",
            OrganisationType.Supplier => "Leverancier",
            _ => organisationType.ToString()
        };

    private static bool Contains(string value, string search) =>
        value.Contains(search, StringComparison.OrdinalIgnoreCase);
}
