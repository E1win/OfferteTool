using Application.Interfaces.Repositories;
using Application.Models.OrganisationManagement;
using Application.Services;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Tests.Application.Services;

public class OrganisationManagementServiceTests
{
    private const string ActorUserId = "actor-1";

    private readonly Mock<IApplicationUserRepository> applicationUserRepository = new();
    private readonly Mock<IOrganisationRepository> organisationRepository = new();
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

    public OrganisationManagementServiceTests()
    {
        userManager
            .Setup(manager => manager.GetRolesAsync(It.IsAny<ApplicationUser>()))
            .Returns((ApplicationUser user) => Task.FromResult(GetRolesFor(user)));
    }

    [Fact]
    public async Task CreateOrganisationAsync_WhenKvkNumberAlreadyExists_ThrowsBusinessRuleViolationException()
    {
        SetupActiveBeheerderActor();
        organisationRepository
            .Setup(repository => repository.ExistsByKvkNumberAsync("12345678", null))
            .ReturnsAsync(true);

        var service = CreateService();
        var request = new CreateOrganisationRequest
        {
            Name = "Nieuwe organisatie",
            KvkNumber = "12345678",
            OrganisationType = OrganisationType.Client
        };

        await Assert.ThrowsAsync<BusinessRuleViolationException>(() => service.CreateOrganisationAsync(request, ActorUserId));

        organisationRepository.Verify(repository => repository.AddAsync(It.IsAny<Organisation>()), Times.Never);
    }

    [Fact]
    public async Task CreateOrganisationAsync_WhenValid_AddsActiveOrganisation()
    {
        SetupActiveBeheerderActor();
        organisationRepository
            .Setup(repository => repository.ExistsByKvkNumberAsync("12345678", null))
            .ReturnsAsync(false);
        organisationRepository
            .Setup(repository => repository.AddAsync(It.IsAny<Organisation>()))
            .ReturnsAsync((Organisation organisation) => organisation);

        var service = CreateService();
        var request = new CreateOrganisationRequest
        {
            Name = " Nieuwe organisatie ",
            KvkNumber = " 12345678 ",
            OrganisationType = OrganisationType.Supplier
        };

        var result = await service.CreateOrganisationAsync(request, ActorUserId);

        Assert.Equal("Nieuwe organisatie", result.Name);
        Assert.Equal("12345678", result.KvkNumber);
        Assert.Equal(OrganisationType.Supplier, result.OrganisationType);
        Assert.True(result.IsActive);
        organisationRepository.Verify(repository => repository.AddAsync(It.Is<Organisation>(organisation =>
            organisation.Name == "Nieuwe organisatie"
            && organisation.KvkNumber == "12345678"
            && organisation.OrganisationType == OrganisationType.Supplier
            && organisation.IsActive)), Times.Once);
    }

    [Fact]
    public async Task DeactivateOrganisationAsync_WhenValid_DeactivatesOrganisationAndLinkedUsers()
    {
        var organisation = CreateOrganisation("Gemeente Utrecht", OrganisationType.Client);
        var linkedUsers = new List<ApplicationUser>
        {
            CreateUser("user-1", organisation),
            CreateUser("user-2", organisation)
        };

        SetupActiveBeheerderActor();
        organisationRepository
            .Setup(repository => repository.GetByIdAsync(organisation.Id))
            .ReturnsAsync(organisation);
        applicationUserRepository
            .Setup(repository => repository.GetByOrganisationAsync(organisation.Id))
            .ReturnsAsync(linkedUsers);

        var service = CreateService();

        await service.DeactivateOrganisationAsync(organisation.Id, ActorUserId);

        Assert.False(organisation.IsActive);
        Assert.All(linkedUsers, user => Assert.False(user.IsActive));
        organisationRepository.Verify(repository => repository.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateOrganisationAsync_WhenIsActiveFalse_DeactivatesLinkedUsers()
    {
        var organisation = CreateOrganisation("Gemeente Utrecht", OrganisationType.Client);
        var linkedUser = CreateUser("user-1", organisation);

        SetupActiveBeheerderActor();
        organisationRepository
            .Setup(repository => repository.GetByIdAsync(organisation.Id))
            .ReturnsAsync(organisation);
        organisationRepository
            .Setup(repository => repository.ExistsByKvkNumberAsync("87654321", organisation.Id))
            .ReturnsAsync(false);
        applicationUserRepository
            .Setup(repository => repository.GetByOrganisationAsync(organisation.Id))
            .ReturnsAsync([linkedUser]);

        var service = CreateService();
        var request = new UpdateOrganisationRequest
        {
            Id = organisation.Id,
            Name = "Gemeente Amsterdam",
            KvkNumber = "87654321",
            OrganisationType = OrganisationType.Client,
            IsActive = false
        };

        var result = await service.UpdateOrganisationAsync(request, ActorUserId);

        Assert.Equal("Gemeente Amsterdam", result.Name);
        Assert.False(result.IsActive);
        Assert.False(linkedUser.IsActive);
        organisationRepository.Verify(repository => repository.SaveChangesAsync(), Times.Once);
    }

    private OrganisationManagementService CreateService()
    {
        return new OrganisationManagementService(
            applicationUserRepository.Object,
            organisationRepository.Object,
            userManager.Object);
    }

    private void SetupActiveBeheerderActor()
    {
        var actor = new ApplicationUser
        {
            Id = ActorUserId,
            UserName = "actor@example.com",
            Email = "actor@example.com",
            FirstName = "Anna",
            LastName = "Jansen",
            IsActive = true
        };

        rolesByUserId[actor.Id] = [Roles.Beheerder];
        applicationUserRepository
            .Setup(repository => repository.GetByIdAsync(ActorUserId))
            .ReturnsAsync(actor);
    }

    private IList<string> GetRolesFor(ApplicationUser user)
    {
        return rolesByUserId.TryGetValue(user.Id, out var roles)
            ? roles
            : [];
    }

    private static ApplicationUser CreateUser(string id, Organisation organisation)
    {
        return new ApplicationUser
        {
            Id = id,
            UserName = $"{id}@example.com",
            Email = $"{id}@example.com",
            FirstName = "Test",
            LastName = "User",
            OrganisationId = organisation.Id,
            Organisation = organisation,
            IsActive = true
        };
    }

    private static Organisation CreateOrganisation(string name, OrganisationType organisationType)
    {
        return new Organisation
        {
            Id = Guid.NewGuid(),
            Name = name,
            KvkNumber = "12345678",
            OrganisationType = organisationType,
            IsActive = true
        };
    }
}
