using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Models.Email;
using Application.Models.UserManagement;
using Application.Services;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Tests.Application.Services;

public class UserManagementServiceTests
{
    private const string ActorUserId = "actor-1";

    private readonly Mock<IApplicationUserRepository> applicationUserRepository = new();
    private readonly Mock<IOrganisationRepository> organisationRepository = new();
    private readonly Mock<IEmailSender> emailSender = new();
    private readonly Mock<UserManager<ApplicationUser>> userManager = new(
        Mock.Of<IUserStore<ApplicationUser>>(),
        null!,
        null!,
        null!,
        null!,
        null!,
        null!,
        null!,
        null!);
    private readonly Dictionary<string, IList<string>> rolesByUserId = [];

    public UserManagementServiceTests()
    {
        userManager
            .Setup(manager => manager.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .Returns((ApplicationUser user) => Task.FromResult(GetRolesFor(user)));
        userManager
            .Setup(manager => manager.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        userManager
            .Setup(manager => manager.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .Callback<ApplicationUser, string>((user, role) => rolesByUserId[user.Id] = [role])
            .ReturnsAsync(IdentityResult.Success);
        userManager
            .Setup(manager => manager.RemoveFromRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .Callback<ApplicationUser, string>((user, role) => rolesByUserId[user.Id] = GetRolesFor(user)
                .Where(existingRole => existingRole != role)
                .ToList())
            .ReturnsAsync(IdentityResult.Success);
        userManager
            .Setup(manager => manager.SetEmailAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .Callback<ApplicationUser, string>((user, email) => user.Email = email)
            .ReturnsAsync(IdentityResult.Success);
        userManager
            .Setup(manager => manager.SetUserNameAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .Callback<ApplicationUser, string>((user, userName) => user.UserName = userName)
            .ReturnsAsync(IdentityResult.Success);
        userManager
            .Setup(manager => manager.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);
        userManager
            .Setup(manager => manager.GetUsersInRoleAsync(Roles.Beheerder))
            .ReturnsAsync([]);
    }

    [Fact]
    public async Task GetUsersAsync_WithSearch_FiltersByNameEmailRoleAndOrganisation()
    {
        // Arrange
        var client = CreateOrganisation("Gemeente Utrecht", OrganisationType.Client);
        var supplier = CreateOrganisation("Bouwbedrijf Noord", OrganisationType.Supplier);
        var inkoper = CreateUser("inkoper-1", "inkoper@example.com", "Iris", "Inkoper", client);
        var leverancier = CreateUser("leverancier-1", "supplier@example.com", "Lars", "Leverancier", supplier);

        SetupRole(inkoper, Roles.Inkoper);
        SetupRole(leverancier, Roles.Leverancier);
        applicationUserRepository
            .Setup(repository => repository.GetAllAsync())
            .ReturnsAsync([inkoper, leverancier]);

        var service = CreateService();

        // Act
        var result = await service.GetUsersAsync(new UserManagementQuery { Search = "utrecht" });

        // Assert
        var user = Assert.Single(result);
        Assert.Equal(inkoper.Id, user.Id);
        Assert.Equal("Gemeente Utrecht", user.OrganisationName);
    }

    [Fact]
    public async Task CreateUserAsync_WhenActorIsNotActiveBeheerder_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var actor = CreateUser(ActorUserId, "actor@example.com", isActive: false);
        SetupRole(actor, Roles.Beheerder);
        applicationUserRepository
            .Setup(repository => repository.GetByIdAsync(ActorUserId))
            .ReturnsAsync(actor);

        var service = CreateService();

        // Act
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreateUserAsync(new CreateUserRequest(), ActorUserId));

        // Assert
        userManager.Verify(manager => manager.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CreateUserAsync_WhenOrganisationTypeDoesNotMatchRole_ThrowsBusinessRuleViolationException()
    {
        // Arrange
        var supplier = CreateOrganisation("Leverancier", OrganisationType.Supplier);
        SetupActiveBeheerderActor();
        organisationRepository
            .Setup(repository => repository.GetByIdAsync(supplier.Id))
            .ReturnsAsync(supplier);

        var service = CreateService();
        var request = new CreateUserRequest
        {
            Email = "inkoper@example.com",
            FirstName = "Iris",
            LastName = "Inkoper",
            Role = Roles.Inkoper,
            OrganisationId = supplier.Id
        };

        // Act
        await Assert.ThrowsAsync<BusinessRuleViolationException>(() => service.CreateUserAsync(request, ActorUserId));

        // Assert
        userManager.Verify(manager => manager.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        emailSender.Verify(sender => sender.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateUserAsync_WhenOrganisationIsInactive_ThrowsBusinessRuleViolationException()
    {
        // Arrange
        var client = CreateOrganisation("Gemeente Utrecht", OrganisationType.Client, isActive: false);
        SetupActiveBeheerderActor();
        organisationRepository
            .Setup(repository => repository.GetByIdAsync(client.Id))
            .ReturnsAsync(client);

        var service = CreateService();
        var request = new CreateUserRequest
        {
            Email = "inkoper@example.com",
            FirstName = "Iris",
            LastName = "Inkoper",
            Role = Roles.Inkoper,
            OrganisationId = client.Id
        };

        // Act
        await Assert.ThrowsAsync<BusinessRuleViolationException>(() => service.CreateUserAsync(request, ActorUserId));

        // Assert
        userManager.Verify(manager => manager.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        emailSender.Verify(sender => sender.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateUserAsync_WhenValid_CreatesIdentityUserAssignsRoleAndSendsEmail()
    {
        // Arrange
        var client = CreateOrganisation("Gemeente Utrecht", OrganisationType.Client);
        SetupActiveBeheerderActor();
        organisationRepository
            .Setup(repository => repository.GetByIdAsync(client.Id))
            .ReturnsAsync(client);

        ApplicationUser? createdUser = null;
        string? generatedPassword = null;
        EmailMessage? sentEmail = null;
        userManager
            .Setup(manager => manager.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .Callback<ApplicationUser, string>((user, password) =>
            {
                createdUser = user;
                generatedPassword = password;
            })
            .ReturnsAsync(IdentityResult.Success);
        emailSender
            .Setup(sender => sender.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<EmailMessage, CancellationToken>((message, _) => sentEmail = message)
            .Returns(Task.CompletedTask);

        var service = CreateService();
        var request = new CreateUserRequest
        {
            Email = " nieuw@example.com ",
            FirstName = " Noor ",
            LastName = " Nieuw ",
            Role = Roles.Inkoper,
            OrganisationId = client.Id
        };

        // Act
        var result = await service.CreateUserAsync(request, ActorUserId);

        // Assert
        Assert.NotNull(createdUser);
        Assert.Equal("nieuw@example.com", createdUser.Email);
        Assert.Equal("nieuw@example.com", createdUser.UserName);
        Assert.Equal("Noor", createdUser.FirstName);
        Assert.Equal("Nieuw", createdUser.LastName);
        Assert.True(createdUser.EmailConfirmed);
        Assert.True(createdUser.IsActive);
        Assert.Equal(client.Id, createdUser.OrganisationId);
        Assert.NotNull(generatedPassword);
        Assert.Contains(generatedPassword, character => !char.IsLetterOrDigit(character));
        Assert.Equal(createdUser.Id, result.User.Id);
        Assert.Equal(Roles.Inkoper, result.User.Role);
        Assert.NotNull(sentEmail);
        Assert.Equal("nieuw@example.com", sentEmail.To);
        Assert.Contains("Wachtwoord:", sentEmail.TextBody);
        userManager.Verify(manager => manager.AddToRoleAsync(createdUser, Roles.Inkoper), Times.Once);
    }

    [Fact]
    public async Task UpdateUserAsync_WhenEmailChanges_UpdatesIdentityEmailUserNameAndConfirmsEmail()
    {
        // Arrange
        var client = CreateOrganisation("Gemeente Utrecht", OrganisationType.Client);
        var user = CreateUser("user-1", "old@example.com", "Old", "Name", client);
        user.EmailConfirmed = false;

        SetupActiveBeheerderActor();
        SetupRole(user, Roles.Inkoper);
        applicationUserRepository
            .Setup(repository => repository.GetByIdAsync(user.Id))
            .ReturnsAsync(user);
        organisationRepository
            .Setup(repository => repository.GetByIdAsync(client.Id))
            .ReturnsAsync(client);

        var service = CreateService();
        var request = new UpdateUserRequest
        {
            UserId = user.Id,
            Email = " new@example.com ",
            FirstName = "New",
            LastName = "Name",
            Role = Roles.Inkoper,
            IsActive = true
        };

        // Act
        var result = await service.UpdateUserAsync(request, ActorUserId);

        // Assert
        Assert.Equal("new@example.com", user.Email);
        Assert.Equal("new@example.com", user.UserName);
        Assert.True(user.EmailConfirmed);
        Assert.Equal("new@example.com", result.Email);
        userManager.Verify(manager => manager.SetEmailAsync(user, "new@example.com"), Times.Once);
        userManager.Verify(manager => manager.SetUserNameAsync(user, "new@example.com"), Times.Once);
        userManager.Verify(manager => manager.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task UpdateUserAsync_WhenLinkedOrganisationIsInactiveAndUserIsActivated_ThrowsBusinessRuleViolationException()
    {
        // Arrange
        var client = CreateOrganisation("Gemeente Utrecht", OrganisationType.Client, isActive: false);
        var user = CreateUser("user-1", "user@example.com", "Inactive", "User", client, isActive: false);

        SetupActiveBeheerderActor();
        SetupRole(user, Roles.Inkoper);
        applicationUserRepository
            .Setup(repository => repository.GetByIdAsync(user.Id))
            .ReturnsAsync(user);
        organisationRepository
            .Setup(repository => repository.GetByIdAsync(client.Id))
            .ReturnsAsync(client);

        var service = CreateService();
        var request = new UpdateUserRequest
        {
            UserId = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = Roles.Inkoper,
            IsActive = true
        };

        // Act
        await Assert.ThrowsAsync<BusinessRuleViolationException>(() => service.UpdateUserAsync(request, ActorUserId));

        // Assert
        Assert.False(user.IsActive);
        userManager.Verify(manager => manager.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task UpdateUserAsync_WhenOrganisationDoesNotMatchNewRole_ThrowsBusinessRuleViolationException()
    {
        // Arrange
        var supplier = CreateOrganisation("Leverancier", OrganisationType.Supplier);
        var user = CreateUser("user-1", "user@example.com", "Lars", "Leverancier", supplier);

        SetupActiveBeheerderActor();
        SetupRole(user, Roles.Leverancier);
        applicationUserRepository
            .Setup(repository => repository.GetByIdAsync(user.Id))
            .ReturnsAsync(user);
        organisationRepository
            .Setup(repository => repository.GetByIdAsync(supplier.Id))
            .ReturnsAsync(supplier);

        var service = CreateService();
        var request = new UpdateUserRequest
        {
            UserId = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = Roles.Inkoper,
            IsActive = true
        };

        // Act
        await Assert.ThrowsAsync<BusinessRuleViolationException>(() => service.UpdateUserAsync(request, ActorUserId));

        // Assert
        userManager.Verify(manager => manager.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
        userManager.Verify(manager => manager.RemoveFromRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateUserAsync_WhenChangingLastActiveBeheerderRole_ThrowsBusinessRuleViolationException()
    {
        // Arrange
        var user = CreateUser("beheerder-1", "beheerder@example.com");

        SetupActiveBeheerderActor();
        SetupRole(user, Roles.Beheerder);
        applicationUserRepository
            .Setup(repository => repository.GetByIdAsync(user.Id))
            .ReturnsAsync(user);
        userManager
            .Setup(manager => manager.GetUsersInRoleAsync(Roles.Beheerder))
            .ReturnsAsync([user]);

        var service = CreateService();
        var request = new UpdateUserRequest
        {
            UserId = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = Roles.Inkoper,
            IsActive = true
        };

        // Act
        await Assert.ThrowsAsync<BusinessRuleViolationException>(() => service.UpdateUserAsync(request, ActorUserId));

        // Assert
        userManager.Verify(manager => manager.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task DisableUserAsync_WhenUserIsActor_ThrowsBusinessRuleViolationException()
    {
        // Arrange
        SetupActiveBeheerderActor();
        var service = CreateService();

        // Act
        await Assert.ThrowsAsync<BusinessRuleViolationException>(() => service.DisableUserAsync(ActorUserId, ActorUserId));

        // Assert
        userManager.Verify(manager => manager.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task DisableUserAsync_WhenValid_SetsIsActiveFalseAndUpdatesIdentityUser()
    {
        // Arrange
        var user = CreateUser("user-1", "user@example.com");

        SetupActiveBeheerderActor();
        SetupRole(user, Roles.Beheerder);
        applicationUserRepository
            .Setup(repository => repository.GetByIdAsync(user.Id))
            .ReturnsAsync(user);
        userManager
            .Setup(manager => manager.GetUsersInRoleAsync(Roles.Beheerder))
            .ReturnsAsync([CreateUser("other-beheerder", "other@example.com"), user]);

        var service = CreateService();

        // Act
        await service.DisableUserAsync(user.Id, ActorUserId);

        // Assert
        Assert.False(user.IsActive);
        userManager.Verify(manager => manager.UpdateAsync(user), Times.Once);
    }

    private UserManagementService CreateService()
    {
        return new UserManagementService(
            applicationUserRepository.Object,
            organisationRepository.Object,
            emailSender.Object,
            userManager.Object);
    }

    private void SetupActiveBeheerderActor()
    {
        var actor = CreateUser(ActorUserId, "actor@example.com");

        SetupRole(actor, Roles.Beheerder);
        applicationUserRepository
            .Setup(repository => repository.GetByIdAsync(ActorUserId))
            .ReturnsAsync(actor);
    }

    private void SetupRole(ApplicationUser user, string role)
    {
        rolesByUserId[user.Id] = [role];
    }

    private IList<string> GetRolesFor(ApplicationUser user)
    {
        return rolesByUserId.TryGetValue(user.Id, out var roles)
            ? roles
            : [];
    }

    private static ApplicationUser CreateUser(
        string id,
        string email,
        string firstName = "Test",
        string lastName = "User",
        Organisation? organisation = null,
        bool isActive = true)
    {
        return new ApplicationUser
        {
            Id = id,
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
            OrganisationId = organisation?.Id,
            Organisation = organisation,
            IsActive = isActive
        };
    }

    private static Organisation CreateOrganisation(string name, OrganisationType organisationType, bool isActive = true)
    {
        return new Organisation
        {
            Id = Guid.NewGuid(),
            Name = name,
            KvkNumber = "12345678",
            OrganisationType = organisationType,
            IsActive = isActive
        };
    }
}
