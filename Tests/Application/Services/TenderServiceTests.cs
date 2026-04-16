using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Services;
using Domain.Constants;
using Domain.Entities;
using Domain.Entities.TenderQuestions;
using Domain.Enums;
using Domain.Exceptions;
using Moq;

namespace Tests.Application.Services;

public class TenderServiceTests
{
    private const string UserId = "user-1";

    private readonly Mock<ITenderRepository> tenderRepository = new();
    private readonly Mock<ICurrentUserService> currentUserService = new();

    [Fact]
    public async Task GetAccessibleTendersAsync_ForInkoperWithOrganisation_ReturnsOrganisationTenders()
    {
        // Arrange
        var organisationId = Guid.NewGuid();
        var expectedTenders = new List<Tender> { CreateTender() };

        SetupCurrentUser(Roles.Inkoper, organisationId);
        tenderRepository
            .Setup(repository => repository.GetByOrganisationAsync(organisationId))
            .ReturnsAsync(expectedTenders);

        var tenderService = CreateTenderService();

        // Act
        var result = await tenderService.GetAccessibleTendersAsync(UserId);

        // Assert
        Assert.Same(expectedTenders, result);
        tenderRepository.Verify(repository => repository.GetByOrganisationAsync(organisationId), Times.Once);
        tenderRepository.Verify(repository => repository.GetPublicOpenAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateTenderAsync_WhenUserIsNotInkoper_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        SetupCurrentUser(Roles.Leverancier);
        var tender = CreateTender();
        var tenderService = CreateTenderService();

        // Act
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => tenderService.CreateTenderAsync(tender, UserId));

        // Assert
        tenderRepository.Verify(repository => repository.AddAsync(It.IsAny<Tender>()), Times.Never);
    }

    [Fact]
    public async Task CreateTenderAsync_WhenInkoperHasNoOrganisation_ThrowsBusinessRuleViolationException()
    {
        // Arrange
        SetupCurrentUser(Roles.Inkoper);
        var tender = CreateTender();
        var tenderService = CreateTenderService();

        // Act
        await Assert.ThrowsAsync<BusinessRuleViolationException>(() => tenderService.CreateTenderAsync(tender, UserId));

        // Assert
        tenderRepository.Verify(repository => repository.AddAsync(It.IsAny<Tender>()), Times.Never);
    }

    [Fact]
    public async Task CreateTenderAsync_WhenValid_SetsSystemFieldsAndPersistsTender()
    {
        // Arrange
        var organisationId = Guid.NewGuid();
        var tender = CreateTender(
            organisationId: Guid.Empty,
            status: TenderStatus.Open,
            startDate: DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            endDate: DateOnly.FromDateTime(DateTime.Today.AddDays(2)));

        SetupCurrentUser(Roles.Inkoper, organisationId);
        tenderRepository
            .Setup(repository => repository.AddAsync(tender))
            .ReturnsAsync(tender);

        var tenderService = CreateTenderService();

        // Act
        var result = await tenderService.CreateTenderAsync(tender, UserId);

        // Assert
        Assert.Same(tender, result);
        Assert.Equal(organisationId, tender.OrganisationId);
        Assert.Equal(TenderStatus.Design, tender.Status);
        tenderRepository.Verify(repository => repository.AddAsync(tender), Times.Once);
    }

    [Fact]
    public async Task GetAccessibleTenderByIdAsync_WhenUserHasNoAccess_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var tender = CreateTender(organisationId: Guid.NewGuid());

        tenderRepository
            .Setup(repository => repository.GetByIdAsync(tender.Id))
            .ReturnsAsync(tender);
        SetupCurrentUser(Roles.Inkoper, Guid.NewGuid());

        var tenderService = CreateTenderService();

        // Act
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => tenderService.GetAccessibleTenderByIdAsync(tender.Id, UserId));

        // Assert
        tenderRepository.Verify(repository => repository.GetByIdAsync(tender.Id), Times.Once);
    }

    [Fact]
    public async Task UpdateTenderAsync_WhenValid_UpdatesAllowedFieldsAndPersists()
    {
        // Arrange
        var organisationId = Guid.NewGuid();
        var existingTender = CreateTender(
            organisationId: organisationId,
            title: "Original",
            description: "Original description",
            startDate: DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            endDate: DateOnly.FromDateTime(DateTime.Today.AddDays(3)),
            isPublic: false);
        var updatedTender = CreateTender(
            organisationId: Guid.NewGuid(),
            status: TenderStatus.Open,
            title: "Updated",
            description: "Updated description",
            startDate: DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
            endDate: DateOnly.FromDateTime(DateTime.Today.AddDays(8)),
            isPublic: true);

        tenderRepository
            .Setup(repository => repository.GetByIdAsync(existingTender.Id))
            .ReturnsAsync(existingTender);
        SetupCurrentUser(Roles.Inkoper, organisationId);

        var tenderService = CreateTenderService();

        // Act
        var result = await tenderService.UpdateTenderAsync(existingTender.Id, updatedTender, UserId);

        // Assert
        Assert.Same(existingTender, result);
        Assert.Equal("Updated", existingTender.Title);
        Assert.Equal("Updated description", existingTender.Description);
        Assert.Equal(updatedTender.StartDate, existingTender.StartDate);
        Assert.Equal(updatedTender.EndDate, existingTender.EndDate);
        Assert.True(existingTender.IsPublic);
        Assert.Equal(organisationId, existingTender.OrganisationId);
        Assert.Equal(TenderStatus.Design, existingTender.Status);
        tenderRepository.Verify(repository => repository.UpdateAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateTenderAsync_WhenTenderIsNotEditable_ThrowsBusinessRuleViolationException()
    {
        // Arrange
        var organisationId = Guid.NewGuid();
        var existingTender = CreateTender(organisationId: organisationId, status: TenderStatus.Open);
        var updatedTender = CreateTender();

        tenderRepository
            .Setup(repository => repository.GetByIdAsync(existingTender.Id))
            .ReturnsAsync(existingTender);
        SetupCurrentUser(Roles.Inkoper, organisationId);

        var tenderService = CreateTenderService();

        // Act
        await Assert.ThrowsAsync<BusinessRuleViolationException>(() => tenderService.UpdateTenderAsync(existingTender.Id, updatedTender, UserId));

        // Assert
        tenderRepository.Verify(repository => repository.UpdateAsync(), Times.Never);
    }

    [Fact]
    public async Task OpenTenderAsync_WhenTenderCanBeOpened_SetsStatusToOpenAndPersists()
    {
        // Arrange
        var organisationId = Guid.NewGuid();
        var tender = CreateTender(
            organisationId: organisationId,
            questions:
            [
                new TextQuestion
                {
                    TenderId = Guid.NewGuid(),
                    Text = "Vraag",
                    Rows = 1
                }
            ]);

        tenderRepository
            .Setup(repository => repository.GetByIdWithQuestionsAndOptionsAsync(tender.Id))
            .ReturnsAsync(tender);
        SetupCurrentUser(Roles.Inkoper, organisationId);

        var tenderService = CreateTenderService();

        // Act
        var result = await tenderService.OpenTenderAsync(tender.Id, UserId);

        // Assert
        Assert.Same(tender, result);
        Assert.Equal(TenderStatus.Open, tender.Status);
        tenderRepository.Verify(repository => repository.UpdateAsync(), Times.Once);
    }

    [Fact]
    public async Task OpenTenderAsync_WhenTenderHasNoQuestions_ThrowsBusinessRuleViolationException()
    {
        // Arrange
        var organisationId = Guid.NewGuid();
        var tender = CreateTender(organisationId: organisationId);

        tenderRepository
            .Setup(repository => repository.GetByIdWithQuestionsAndOptionsAsync(tender.Id))
            .ReturnsAsync(tender);
        SetupCurrentUser(Roles.Inkoper, organisationId);

        var tenderService = CreateTenderService();

        // Act
        await Assert.ThrowsAsync<BusinessRuleViolationException>(() => tenderService.OpenTenderAsync(tender.Id, UserId));

        // Assert
        Assert.Equal(TenderStatus.Design, tender.Status);
        tenderRepository.Verify(repository => repository.UpdateAsync(), Times.Never);
    }

    private TenderService CreateTenderService()
    {
        return new TenderService(tenderRepository.Object, currentUserService.Object);
    }

    private void SetupCurrentUser(string role, Guid? organisationId = null)
    {
        currentUserService
            .Setup(service => service.GetUserWithRoleAsync(UserId))
            .ReturnsAsync((CreateUser(organisationId), role));
    }

    private static ApplicationUser CreateUser(Guid? organisationId = null)
    {
        return new ApplicationUser
        {
            Id = UserId,
            UserName = "test@example.com",
            Email = "test@example.com",
            OrganisationId = organisationId
        };
    }

    private static Tender CreateTender(
        Guid? organisationId = null,
        TenderStatus status = TenderStatus.Design,
        string title = "Tender",
        string description = "Description",
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        bool isPublic = false,
        List<TenderQuestion>? questions = null)
    {
        return new Tender
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            StartDate = startDate ?? DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            EndDate = endDate ?? DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
            Status = status,
            IsPublic = isPublic,
            OrganisationId = organisationId ?? Guid.NewGuid(),
            Questions = questions ?? []
        };
    }
}
