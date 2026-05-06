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
    private readonly Mock<ITenderChangeLogRepository> tenderChangeLogRepository = new();

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
        tenderRepository.Verify(repository => repository.GetClosedByReviewerAsync(It.IsAny<string>()), Times.Never);
        tenderRepository.Verify(repository => repository.GetPublicOpenAsync(), Times.Never);
    }

    [Fact]
    public async Task GetAccessibleTendersAsync_ForBeoordelaar_ReturnsClosedAssignedTenders()
    {
        // Arrange
        var expectedTenders = new List<Tender> { CreateTender(status: TenderStatus.Closed) };

        SetupCurrentUser(Roles.Beoordelaar, Guid.NewGuid());
        tenderRepository
            .Setup(repository => repository.GetClosedByReviewerAsync(UserId))
            .ReturnsAsync(expectedTenders);

        var tenderService = CreateTenderService();

        // Act
        var result = await tenderService.GetAccessibleTendersAsync(UserId);

        // Assert
        Assert.Same(expectedTenders, result);
        tenderRepository.Verify(repository => repository.GetClosedByReviewerAsync(UserId), Times.Once);
        tenderRepository.Verify(repository => repository.GetByOrganisationAsync(It.IsAny<Guid>()), Times.Never);
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
        Assert.Contains(tender.Reviewers, reviewer => reviewer.UserId == UserId);
        tenderRepository.Verify(repository => repository.AddAsync(tender), Times.Once);
    }

    [Fact]
    public async Task GetAccessibleTenderByIdAsync_WhenUserHasNoAccess_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var tender = CreateTender(organisationId: Guid.NewGuid());

        tenderRepository
            .Setup(repository => repository.GetByIdWithReviewersAsync(tender.Id))
            .ReturnsAsync(tender);
        SetupCurrentUser(Roles.Inkoper, Guid.NewGuid());

        var tenderService = CreateTenderService();

        // Act
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => tenderService.GetAccessibleTenderByIdAsync(tender.Id, UserId));

        // Assert
        tenderRepository.Verify(repository => repository.GetByIdWithReviewersAsync(tender.Id), Times.Once);
    }

    [Fact]
    public async Task GetAccessibleTenderByIdAsync_ForAssignedBeoordelaarOnClosedTender_ReturnsTender()
    {
        // Arrange
        var organisationId = Guid.NewGuid();
        var tender = CreateTender(organisationId: organisationId, status: TenderStatus.Closed);
        tender.Reviewers.Add(new TenderReviewer(UserId)
        {
            TenderId = tender.Id
        });

        tenderRepository
            .Setup(repository => repository.GetByIdWithReviewersAsync(tender.Id))
            .ReturnsAsync(tender);
        SetupCurrentUser(Roles.Beoordelaar, organisationId);

        var tenderService = CreateTenderService();

        // Act
        var result = await tenderService.GetAccessibleTenderByIdAsync(tender.Id, UserId);

        // Assert
        Assert.Same(tender, result);
    }

    [Fact]
    public async Task GetAccessibleTenderByIdAsync_ForAssignedBeoordelaarOnOpenTender_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var organisationId = Guid.NewGuid();
        var tender = CreateTender(organisationId: organisationId, status: TenderStatus.Open);
        tender.Reviewers.Add(new TenderReviewer(UserId)
        {
            TenderId = tender.Id
        });

        tenderRepository
            .Setup(repository => repository.GetByIdWithReviewersAsync(tender.Id))
            .ReturnsAsync(tender);
        SetupCurrentUser(Roles.Beoordelaar, organisationId);

        var tenderService = CreateTenderService();

        // Act
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => tenderService.GetAccessibleTenderByIdAsync(tender.Id, UserId));
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
            endDate: DateOnly.FromDateTime(DateTime.Today.AddDays(3)),
            isPublic: false);
        var updatedTender = CreateTender(
            organisationId: Guid.NewGuid(),
            status: TenderStatus.Open,
            title: "Updated",
            description: "Updated description",
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

    [Fact]
    public async Task CloseTenderAsync_WhenTenderIsOpen_SetsStatusToClosedAndPersists()
    {
        // Arrange
        var organisationId = Guid.NewGuid();
        var tender = CreateTender(organisationId: organisationId, status: TenderStatus.Open);

        tenderRepository
            .Setup(repository => repository.GetByIdAsync(tender.Id))
            .ReturnsAsync(tender);
        SetupCurrentUser(Roles.Inkoper, organisationId);

        var tenderService = CreateTenderService();

        // Act
        var result = await tenderService.CloseTenderAsync(tender.Id, UserId);

        // Assert
        Assert.Same(tender, result);
        Assert.Equal(TenderStatus.Closed, tender.Status);
        tenderRepository.Verify(repository => repository.UpdateAsync(), Times.Once);
    }

    [Fact]
    public async Task CloseTenderAsync_WhenTenderIsNotOpen_ThrowsBusinessRuleViolationException()
    {
        // Arrange
        var organisationId = Guid.NewGuid();
        var tender = CreateTender(organisationId: organisationId, status: TenderStatus.Design);

        tenderRepository
            .Setup(repository => repository.GetByIdAsync(tender.Id))
            .ReturnsAsync(tender);
        SetupCurrentUser(Roles.Inkoper, organisationId);

        var tenderService = CreateTenderService();

        // Act
        await Assert.ThrowsAsync<BusinessRuleViolationException>(() => tenderService.CloseTenderAsync(tender.Id, UserId));

        // Assert
        Assert.Equal(TenderStatus.Design, tender.Status);
        tenderRepository.Verify(repository => repository.UpdateAsync(), Times.Never);
    }

    [Fact]
    public async Task CompleteTenderAsync_WhenTenderIsClosed_SetsStatusToCompletedAndPersists()
    {
        // Arrange
        var organisationId = Guid.NewGuid();
        var tender = CreateTender(organisationId: organisationId, status: TenderStatus.Closed);

        tenderRepository
            .Setup(repository => repository.GetByIdAsync(tender.Id))
            .ReturnsAsync(tender);
        SetupCurrentUser(Roles.Inkoper, organisationId);

        var tenderService = CreateTenderService();

        // Act
        var result = await tenderService.CompleteTenderAsync(tender.Id, UserId);

        // Assert
        Assert.Same(tender, result);
        Assert.Equal(TenderStatus.Completed, tender.Status);
        tenderRepository.Verify(repository => repository.UpdateAsync(), Times.Once);
    }

    [Fact]
    public async Task CompleteTenderAsync_WhenTenderIsNotClosed_ThrowsBusinessRuleViolationException()
    {
        // Arrange
        var organisationId = Guid.NewGuid();
        var tender = CreateTender(organisationId: organisationId, status: TenderStatus.Open);

        tenderRepository
            .Setup(repository => repository.GetByIdAsync(tender.Id))
            .ReturnsAsync(tender);
        SetupCurrentUser(Roles.Inkoper, organisationId);

        var tenderService = CreateTenderService();

        // Act
        await Assert.ThrowsAsync<BusinessRuleViolationException>(() => tenderService.CompleteTenderAsync(tender.Id, UserId));

        // Assert
        Assert.Equal(TenderStatus.Open, tender.Status);
        tenderRepository.Verify(repository => repository.UpdateAsync(), Times.Never);
    }

    [Fact]
    public async Task AmendTenderDetailsAsync_WhenTenderIsOpen_UpdatesTitleDescriptionAndLogsChanges()
    {
        // Arrange
        var organisationId = Guid.NewGuid();
        var existingTender = CreateTender(
            organisationId: organisationId,
            status: TenderStatus.Open,
            title: "Oude titel",
            description: "Oude beschrijving");

        tenderRepository
            .Setup(repository => repository.GetByIdAsync(existingTender.Id))
            .ReturnsAsync(existingTender);
        SetupCurrentUser(Roles.Inkoper, organisationId);

        var tenderService = CreateTenderService();

        // Act
        var result = await tenderService.AmendTenderDetailsAsync(
            existingTender.Id,
            new global::Application.Models.Tender.TenderDetailsAmendment
            {
                Title = "Nieuwe titel",
                Description = "Nieuwe beschrijving"
            },
            UserId);

        // Assert
        Assert.Same(existingTender, result);
        Assert.Equal("Nieuwe titel", existingTender.Title);
        Assert.Equal("Nieuwe beschrijving", existingTender.Description);
        tenderChangeLogRepository.Verify(repository => repository.AddAsync(It.Is<TenderChangeLog>(changeLog =>
            changeLog.Type == TenderChangeLogType.TenderTitleAmended
            && changeLog.OldValue == "Oude titel"
            && changeLog.NewValue == "Nieuwe titel")), Times.Once);
        tenderChangeLogRepository.Verify(repository => repository.AddAsync(It.Is<TenderChangeLog>(changeLog =>
            changeLog.Type == TenderChangeLogType.TenderDescriptionAmended
            && changeLog.OldValue == "Oude beschrijving"
            && changeLog.NewValue == "Nieuwe beschrijving")), Times.Once);
        tenderChangeLogRepository.Verify(repository => repository.SaveChangesAsync(), Times.Once);
        tenderRepository.Verify(repository => repository.UpdateAsync(), Times.Never);
    }

    [Fact]
    public async Task AmendTenderDetailsAsync_WhenTenderIsNotOpen_ThrowsBusinessRuleViolationException()
    {
        // Arrange
        var organisationId = Guid.NewGuid();
        var existingTender = CreateTender(organisationId: organisationId, status: TenderStatus.Design);

        tenderRepository
            .Setup(repository => repository.GetByIdAsync(existingTender.Id))
            .ReturnsAsync(existingTender);
        SetupCurrentUser(Roles.Inkoper, organisationId);

        var tenderService = CreateTenderService();

        // Act
        await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            tenderService.AmendTenderDetailsAsync(
                existingTender.Id,
                new global::Application.Models.Tender.TenderDetailsAmendment
                {
                    Title = "Nieuwe titel",
                    Description = "Nieuwe beschrijving"
                },
                UserId));

        // Assert
        tenderChangeLogRepository.Verify(repository => repository.AddAsync(It.IsAny<TenderChangeLog>()), Times.Never);
        tenderChangeLogRepository.Verify(repository => repository.SaveChangesAsync(), Times.Never);
    }

    private TenderService CreateTenderService()
    {
        return new TenderService(
            tenderRepository.Object,
            currentUserService.Object,
            tenderChangeLogRepository.Object);
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
        DateOnly? endDate = null,
        bool isPublic = false,
        List<TenderQuestion>? questions = null)
    {
        return new Tender
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            EndDate = endDate ?? DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
            Status = status,
            IsPublic = isPublic,
            OrganisationId = organisationId ?? Guid.NewGuid(),
            Questions = questions ?? []
        };
    }
}
