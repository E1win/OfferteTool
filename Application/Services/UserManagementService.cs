using System.Security.Cryptography;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Models.Email;
using Application.Models.UserManagement;
using Domain.Constants;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.AspNetCore.Identity;

namespace Application.Services;

public class UserManagementService(
    IApplicationUserRepository applicationUserRepository,
    IOrganisationRepository organisationRepository,
    IEmailSender emailSender,
    UserManager<ApplicationUser> userManager) : IUserManagementService
{
    private static readonly string[] ManageableRoles =
    [
        Roles.Inkoper,
        Roles.Beoordelaar,
        Roles.Beheerder,
        Roles.Leverancier
    ];

    public async Task<List<ManagedUser>> GetUsersAsync(UserManagementQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        var users = await applicationUserRepository.GetAllAsync();
        var managedUsers = new List<ManagedUser>();

        foreach (var user in users)
            managedUsers.Add(await MapToManagedUserAsync(user));

        return ApplySearch(managedUsers, query.Search);
    }

    public async Task<ManagedUser> GetUserAsync(string userId)
    {
        var user = await applicationUserRepository.GetByIdAsync(userId)
            ?? throw new BusinessRuleViolationException("De gebruiker bestaat niet.");

        return await MapToManagedUserAsync(user);
    }

    public async Task<UserManagementFormOptions> GetFormOptionsAsync()
    {
        var organisations = await organisationRepository.GetAllAsync();

        return new UserManagementFormOptions
        {
            Roles = ManageableRoles
                .Select(role => new UserRoleOption
                {
                    Value = role,
                    Label = role
                })
                .ToList(),
            Organisations = organisations
                .Select(organisation => new UserOrganisationOption
                {
                    Id = organisation.Id,
                    Name = organisation.Name,
                    OrganisationType = organisation.OrganisationType
                })
                .ToList()
        };
    }

    public async Task<CreateUserResult> CreateUserAsync(CreateUserRequest request, string actorUserId)
    {
        await EnsureActorIsBeheerderAsync(actorUserId);
        ArgumentNullException.ThrowIfNull(request);

        var organisationId = await ValidateOrganisationAssignmentAsync(request.Role, request.OrganisationId);
        var password = GeneratePassword();
        var user = new ApplicationUser
        {
            UserName = NormalizeRequiredText(request.Email),
            Email = NormalizeRequiredText(request.Email),
            EmailConfirmed = true,
            FirstName = NormalizeRequiredText(request.FirstName),
            LastName = NormalizeRequiredText(request.LastName),
            OrganisationId = organisationId,
            IsActive = true
        };

        var createResult = await userManager.CreateAsync(user, password);
        EnsureIdentitySucceeded(createResult, "De gebruiker kon niet worden aangemaakt.");

        var roleResult = await userManager.AddToRoleAsync(user, request.Role);
        EnsureIdentitySucceeded(roleResult, "De rol kon niet aan de gebruiker worden gekoppeld.");

        await emailSender.SendAsync(CreateAccountCreatedEmail(user, password));

        return new CreateUserResult
        {
            User = await MapToManagedUserAsync(user)
        };
    }

    public async Task<ManagedUser> UpdateUserAsync(UpdateUserRequest request, string actorUserId)
    {
        await EnsureActorIsBeheerderAsync(actorUserId);
        ArgumentNullException.ThrowIfNull(request);

        var user = await applicationUserRepository.GetByIdAsync(request.UserId)
            ?? throw new BusinessRuleViolationException("De gebruiker bestaat niet.");
        var currentRole = await GetSingleRoleAsync(user);

        if (user.Id == actorUserId && !request.IsActive)
            throw new BusinessRuleViolationException("U kunt uw eigen account niet uitschakelen.");

        await EnsureBeheerderCanBeChangedAsync(user, currentRole, request.Role, request.IsActive);

        await ValidateExistingOrganisationAssignmentAsync(user, request.Role, request.IsActive);
        var email = NormalizeRequiredText(request.Email);
        var emailChanged = !string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase);

        user.FirstName = NormalizeRequiredText(request.FirstName);
        user.LastName = NormalizeRequiredText(request.LastName);
        user.IsActive = request.IsActive;

        if (emailChanged)
            await UpdateEmailAsync(user, email);

        var updateResult = await userManager.UpdateAsync(user);
        EnsureIdentitySucceeded(updateResult, "De gebruiker kon niet worden bijgewerkt.");

        if (currentRole != request.Role)
        {
            var removeResult = await userManager.RemoveFromRoleAsync(user, currentRole);
            EnsureIdentitySucceeded(removeResult, "De oude rol kon niet worden verwijderd.");

            var addResult = await userManager.AddToRoleAsync(user, request.Role);
            EnsureIdentitySucceeded(addResult, "De nieuwe rol kon niet worden gekoppeld.");
        }

        return await MapToManagedUserAsync(user);
    }

    private async Task UpdateEmailAsync(ApplicationUser user, string email)
    {
        var emailResult = await userManager.SetEmailAsync(user, email);
        EnsureIdentitySucceeded(emailResult, "Het e-mailadres kon niet worden bijgewerkt.");

        var userNameResult = await userManager.SetUserNameAsync(user, email);
        EnsureIdentitySucceeded(userNameResult, "De gebruikersnaam kon niet worden bijgewerkt.");

        user.EmailConfirmed = true;
    }

    public async Task DisableUserAsync(string userId, string actorUserId)
    {
        await EnsureActorIsBeheerderAsync(actorUserId);

        if (userId == actorUserId)
            throw new BusinessRuleViolationException("U kunt uw eigen account niet uitschakelen.");

        var user = await applicationUserRepository.GetByIdAsync(userId)
            ?? throw new BusinessRuleViolationException("De gebruiker bestaat niet.");
        var currentRole = await GetSingleRoleAsync(user);

        await EnsureBeheerderCanBeChangedAsync(user, currentRole, currentRole, isActive: false);

        user.IsActive = false;
        var result = await userManager.UpdateAsync(user);
        EnsureIdentitySucceeded(result, "De gebruiker kon niet worden uitgeschakeld.");
    }

    private async Task EnsureActorIsBeheerderAsync(string actorUserId)
    {
        var actor = await applicationUserRepository.GetByIdAsync(actorUserId)
            ?? throw new UnauthorizedAccessException("U bent niet gemachtigd om gebruikers te beheren.");
        var actorRoles = await userManager.GetRolesAsync(actor);

        if (!actor.IsActive || !actorRoles.Contains(Roles.Beheerder))
            throw new UnauthorizedAccessException("U bent niet gemachtigd om gebruikers te beheren.");
    }

    private async Task<Guid?> ValidateOrganisationAssignmentAsync(string role, Guid? organisationId)
    {
        if (!ManageableRoles.Contains(role))
            throw new BusinessRuleViolationException("De opgegeven rol is ongeldig.");

        var user = new ApplicationUser();

        if (!user.RequiresOrganisation(role))
        {
            if (organisationId is not null)
                throw new BusinessRuleViolationException("Een beheerder wordt niet aan een organisatie gekoppeld.");

            return null;
        }

        if (organisationId is null)
            throw new BusinessRuleViolationException("Kies een organisatie voor deze rol.");

        var organisation = await organisationRepository.GetByIdAsync(organisationId.Value)
            ?? throw new BusinessRuleViolationException("De gekozen organisatie bestaat niet.");

        if (!organisation.IsActive)
            throw new BusinessRuleViolationException("Kies een actieve organisatie voor deze gebruiker.");

        if (!user.CanAttachToOrganisation(role, organisation.OrganisationType))
            throw new BusinessRuleViolationException("De gekozen organisatie past niet bij deze rol.");

        return organisation.Id;
    }

    private async Task ValidateExistingOrganisationAssignmentAsync(ApplicationUser user, string role, bool isActive)
    {
        if (!ManageableRoles.Contains(role))
            throw new BusinessRuleViolationException("De opgegeven rol is ongeldig.");

        if (!user.RequiresOrganisation(role))
        {
            if (user.OrganisationId is not null)
                throw new BusinessRuleViolationException("Een beheerder kan niet aan een organisatie gekoppeld blijven.");

            return;
        }

        if (user.OrganisationId is null)
            throw new BusinessRuleViolationException("Deze gebruiker heeft geen organisatie. Maak een nieuwe gebruiker aan met de juiste organisatie.");

        var organisation = await organisationRepository.GetByIdAsync(user.OrganisationId.Value)
            ?? throw new BusinessRuleViolationException("De gekoppelde organisatie bestaat niet.");

        if (isActive && !organisation.IsActive)
            throw new BusinessRuleViolationException("Deze gebruiker kan niet worden geactiveerd zolang de gekoppelde organisatie inactief is.");

        if (!user.CanAttachToOrganisation(role, organisation.OrganisationType))
            throw new BusinessRuleViolationException("De bestaande organisatie past niet bij deze rol.");
    }

    private async Task EnsureBeheerderCanBeChangedAsync(
        ApplicationUser user,
        string currentRole,
        string requestedRole,
        bool isActive)
    {
        if (!user.IsActive || currentRole != Roles.Beheerder)
            return;

        if (requestedRole == Roles.Beheerder && isActive)
            return;

        var activeBeheerders = await GetActiveBeheerderCountAsync();

        if (activeBeheerders <= 1)
            throw new BusinessRuleViolationException("De laatste actieve beheerder kan niet worden uitgeschakeld of van rol worden gewijzigd.");
    }

    private async Task<int> GetActiveBeheerderCountAsync()
    {
        var beheerders = await userManager.GetUsersInRoleAsync(Roles.Beheerder);
        return beheerders.Count(user => user.IsActive);
    }

    private async Task<ManagedUser> MapToManagedUserAsync(ApplicationUser user)
    {
        var role = await GetSingleRoleAsync(user);

        return new ManagedUser
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = role,
            IsActive = user.IsActive,
            OrganisationId = user.OrganisationId,
            OrganisationName = user.Organisation?.Name
                ?? (user.OrganisationId is null
                    ? null
                    : (await organisationRepository.GetByIdAsync(user.OrganisationId.Value))?.Name)
        };
    }

    private async Task<string> GetSingleRoleAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);

        if (roles.Count != 1)
            throw new BusinessRuleViolationException("De gebruiker heeft niet precies een rol.");

        return roles[0];
    }

    private static string GeneratePassword()
    {
        const string uppercase = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lowercase = "abcdefghijkmnopqrstuvwxyz";
        const string digits = "23456789";
        const string specialCharacters = "!#$%&*+-?@";
        const string allCharacters = uppercase + lowercase + digits + specialCharacters;

        var requiredCharacters = new[]
        {
            GetRandomCharacter(uppercase),
            GetRandomCharacter(lowercase),
            GetRandomCharacter(digits),
            GetRandomCharacter(specialCharacters)
        };

        var passwordCharacters = requiredCharacters
            .Concat(Enumerable.Range(0, 12).Select(_ => GetRandomCharacter(allCharacters)))
            .OrderBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue))
            .ToArray();

        return new string(passwordCharacters);
    }

    private static char GetRandomCharacter(string characters) =>
        characters[RandomNumberGenerator.GetInt32(characters.Length)];

    private static string NormalizeRequiredText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new BusinessRuleViolationException("Vul alle verplichte gebruikersgegevens in.");

        return value.Trim();
    }

    private static void EnsureIdentitySucceeded(IdentityResult result, string fallbackMessage)
    {
        if (result.Succeeded)
            return;

        var errors = result.Errors.Select(error => error.Description).ToList();

        throw new BusinessRuleViolationException(errors.Count == 0
            ? fallbackMessage
            : string.Join(" ", errors));
    }

    private static EmailMessage CreateAccountCreatedEmail(ApplicationUser user, string password)
    {
        var name = string.IsNullOrWhiteSpace(user.FirstName)
            ? "gebruiker"
            : user.FirstName;

        return new EmailMessage
        {
            To = user.Email ?? throw new BusinessRuleViolationException("De gebruiker heeft geen e-mailadres."),
            Subject = "Er is een account voor u aangemaakt in OfferteTool",
            TextBody = $"""
                Beste {name},

                Er is een account voor u aangemaakt in OfferteTool.

                U kunt inloggen met:
                E-mailadres: {user.Email}
                Wachtwoord: {password}

                Er wordt aangeraden om het wachtwoord z.s.m. na het inloggen via uw profiel te wijzigen. Bewaar dit bericht zorgvuldig en deel uw wachtwoord niet met anderen.

                Met vriendelijke groet,
                OfferteTool
                """,
            HtmlBody = $"""
                <p>Beste {System.Net.WebUtility.HtmlEncode(name)},</p>
                <p>Er is een account voor u aangemaakt in OfferteTool.</p>
                <p>
                    U kunt inloggen met:<br>
                    E-mailadres: {System.Net.WebUtility.HtmlEncode(user.Email)}<br>
                    Wachtwoord: {System.Net.WebUtility.HtmlEncode(password)}
                </p>
                <p>Er wordt aangeraden om het wachtwoord z.s.m. na het inloggen via uw profiel te wijzigen. Bewaar dit bericht zorgvuldig en deel uw wachtwoord niet met anderen.</p>
                <p>Met vriendelijke groet,<br>OfferteTool</p>
                """
        };
    }

    private static List<ManagedUser> ApplySearch(List<ManagedUser> users, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return users;

        var normalizedSearch = search.Trim();

        return users
            .Where(user =>
                Contains(user.FirstName, normalizedSearch)
                || Contains(user.LastName, normalizedSearch)
                || Contains(user.Email, normalizedSearch)
                || Contains(user.Role, normalizedSearch)
                || Contains(user.OrganisationName, normalizedSearch))
            .ToList();
    }

    private static bool Contains(string? value, string search) =>
        value?.Contains(search, StringComparison.OrdinalIgnoreCase) == true;
}
