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

public class TenderQuestionServiceTests
{
    private const string UserId = "user-1";

    private readonly Mock<ITenderRepository> tenderRepository = new();
    private readonly Mock<ICurrentUserService> currentUserService = new();
    private readonly Mock<ITenderQuestionRepository> tenderQuestionRepository = new();

    [Fact]
    public async Task GetQuestionsAsync_WhenUserHasAccess_ReturnsQuestionsOrderedByOrder()
    {
        // Arrange
        var organisationId = Guid.NewGuid();
        var tender = CreateTender(
            organisationId: organisationId,
            questions:
            [
                CreateTextQuestion(order: 2, text: "Vraag 3"),
                CreateTextQuestion(order: 0, text: "Vraag 1"),
                CreateTextQuestion(order: 1, text: "Vraag 2")
            ]);

        tenderRepository
            .Setup(repository => repository.GetByIdWithQuestionsAndOptionsAsync(tender.Id))
            .ReturnsAsync(tender);
        SetupCurrentUser(Roles.Inkoper, organisationId);

        var tenderQuestionService = CreateTenderQuestionService();

        // Act
        var result = await tenderQuestionService.GetQuestionsAsync(tender.Id, UserId);

        // Assert
        Assert.Collection(
            result,
            question => Assert.Equal(0, question.Order),
            question => Assert.Equal(1, question.Order),
            question => Assert.Equal(2, question.Order));
    }

    [Fact]
    public async Task CreateQuestionAsync_WhenValid_SetsTenderIdAndOrderAndPersistsQuestion()
    {
        // Arrange
        var organisationId = Guid.NewGuid();
        var tender = CreateTender(organisationId: organisationId);
        var question = new ChoiceQuestion
        {
            TenderId = Guid.Empty,
            Text = "Welke optie kiest u?",
            AllowMultipleSelection = false,
            Options =
            [
                new TenderQuestionOption { Id = Guid.Empty, Text = "Optie A", Order = 99 },
                new TenderQuestionOption { Id = Guid.Empty, Text = "Optie B", Order = 42 }
            ]
        };

        tenderRepository
            .Setup(repository => repository.GetByIdAsync(tender.Id))
            .ReturnsAsync(tender);
        tenderQuestionRepository
            .Setup(repository => repository.GetNextOrderForTenderAsync(tender.Id))
            .ReturnsAsync(3);
        tenderQuestionRepository
            .Setup(repository => repository.AddAsync(question))
            .ReturnsAsync(question);
        SetupCurrentUser(Roles.Inkoper, organisationId);

        var tenderQuestionService = CreateTenderQuestionService();

        // Act
        var result = await tenderQuestionService.CreateQuestionAsync(tender.Id, question, UserId);

        // Assert
        Assert.Same(question, result);
        Assert.Equal(tender.Id, question.TenderId);
        Assert.Equal(3, question.Order);
        Assert.Collection(
            question.Options.OrderBy(option => option.Order),
            option => Assert.Equal(0, option.Order),
            option => Assert.Equal(1, option.Order));
        tenderQuestionRepository.Verify(repository => repository.AddAsync(question), Times.Once);
    }

    [Fact]
    public async Task UpdateQuestionAsync_WhenTenderIsNotEditable_ThrowsBusinessRuleViolationException()
    {
        // Arrange
        var organisationId = Guid.NewGuid();
        var tender = CreateTender(organisationId: organisationId, status: TenderStatus.Open);
        var existingQuestion = CreateTextQuestion(tenderId: tender.Id, order: 0, text: "Huidige vraag");
        var updatedQuestion = CreateTextQuestion(tenderId: tender.Id, order: 0, text: "Bijgewerkte vraag");

        tenderQuestionRepository
            .Setup(repository => repository.GetByIdAsync(existingQuestion.Id))
            .ReturnsAsync(existingQuestion);
        tenderRepository
            .Setup(repository => repository.GetByIdAsync(tender.Id))
            .ReturnsAsync(tender);
        SetupCurrentUser(Roles.Inkoper, organisationId);

        var tenderQuestionService = CreateTenderQuestionService();

        // Act
        await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            tenderQuestionService.UpdateQuestionAsync(tender.Id, existingQuestion.Id, updatedQuestion, UserId));

        // Assert
        tenderQuestionRepository.Verify(repository => repository.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task ReorderQuestionsAsync_WhenOrderedIdsDoNotMatchQuestions_ThrowsBusinessRuleViolationException()
    {
        // Arrange
        var organisationId = Guid.NewGuid();
        var tender = CreateTender(
            organisationId: organisationId,
            questions:
            [
                CreateTextQuestion(order: 0),
                CreateTextQuestion(order: 1)
            ]);

        tenderRepository
            .Setup(repository => repository.GetByIdWithQuestionsAndOptionsAsync(tender.Id))
            .ReturnsAsync(tender);
        SetupCurrentUser(Roles.Inkoper, organisationId);

        var tenderQuestionService = CreateTenderQuestionService();
        var orderedQuestionIds = new List<Guid> { tender.Questions[0].Id };

        // Act
        await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            tenderQuestionService.ReorderQuestionsAsync(tender.Id, orderedQuestionIds, UserId));

        // Assert
        tenderQuestionRepository.Verify(repository => repository.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task ReorderQuestionsAsync_WhenValid_ReordersQuestionsAndSavesTwice()
    {
        // Arrange
        var organisationId = Guid.NewGuid();
        var firstQuestion = CreateTextQuestion(order: 0, text: "Vraag 1");
        var secondQuestion = CreateTextQuestion(order: 1, text: "Vraag 2");
        var tender = CreateTender(
            organisationId: organisationId,
            questions: [firstQuestion, secondQuestion]);

        tenderRepository
            .Setup(repository => repository.GetByIdWithQuestionsAndOptionsAsync(tender.Id))
            .ReturnsAsync(tender);
        SetupCurrentUser(Roles.Inkoper, organisationId);

        var tenderQuestionService = CreateTenderQuestionService();
        var orderedQuestionIds = new List<Guid> { secondQuestion.Id, firstQuestion.Id };

        // Act
        await tenderQuestionService.ReorderQuestionsAsync(tender.Id, orderedQuestionIds, UserId);

        // Assert
        Assert.Equal(1, firstQuestion.Order);
        Assert.Equal(0, secondQuestion.Order);
        tenderQuestionRepository.Verify(repository => repository.SaveChangesAsync(), Times.Exactly(2));
    }

    private TenderQuestionService CreateTenderQuestionService()
    {
        return new TenderQuestionService(
            tenderRepository.Object,
            currentUserService.Object,
            tenderQuestionRepository.Object);
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
        List<TenderQuestion>? questions = null)
    {
        var tender = new Tender
        {
            Id = Guid.NewGuid(),
            Title = "Tender",
            Description = "Description",
            StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
            Status = status,
            IsPublic = false,
            OrganisationId = organisationId ?? Guid.NewGuid(),
            Questions = questions ?? []
        };

        foreach (var question in tender.Questions)
            question.TenderId = tender.Id;

        return tender;
    }

    private static TextQuestion CreateTextQuestion(Guid? tenderId = null, int order = 0, string text = "Vraag")
    {
        return new TextQuestion
        {
            Id = Guid.NewGuid(),
            TenderId = tenderId ?? Guid.NewGuid(),
            Order = order,
            Text = text,
            Rows = 1
        };
    }
}
