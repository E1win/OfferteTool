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

public class TenderComparisonServiceTests
{
    private const string UserId = "user-1";

    private readonly Mock<ITenderRepository> tenderRepository = new();
    private readonly Mock<ICurrentUserService> currentUserService = new();

    [Fact]
    public async Task GetTenderComparisonDashboardAsync_WhenTenderIsNotCompleted_ThrowsBusinessRuleViolationException()
    {
        // Arrange
        var organisationId = Guid.NewGuid();
        var tender = CreateTender(organisationId: organisationId, status: TenderStatus.Closed);

        tenderRepository
            .Setup(repository => repository.GetByIdWithComparisonDataAsync(tender.Id))
            .ReturnsAsync(tender);
        SetupCurrentUser(Roles.Inkoper, organisationId);

        var service = CreateTenderComparisonService();

        // Act
        await Assert.ThrowsAsync<BusinessRuleViolationException>(() => service.GetTenderComparisonDashboardAsync(tender.Id, UserId));
    }

    [Fact]
    public async Task GetTenderComparisonDashboardAsync_WhenUserCannotManageTender_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var tender = CreateTender(organisationId: Guid.NewGuid(), status: TenderStatus.Completed);

        tenderRepository
            .Setup(repository => repository.GetByIdWithComparisonDataAsync(tender.Id))
            .ReturnsAsync(tender);
        SetupCurrentUser(Roles.Inkoper, Guid.NewGuid());

        var service = CreateTenderComparisonService();

        // Act
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetTenderComparisonDashboardAsync(tender.Id, UserId));
    }

    [Fact]
    public async Task GetTenderComparisonDashboardAsync_WhenTenderIsCompleted_ReturnsRankedSubmissions()
    {
        // Arrange
        var organisationId = Guid.NewGuid();
        var firstQuestion = CreateScoredQuestion(order: 0, score: 10);
        var secondQuestion = CreateScoredQuestion(order: 1, score: 20);
        var alphaSubmission = CreateSubmission("Alpha BV", DateTime.UtcNow.AddMinutes(-2));
        var betaSubmission = CreateSubmission("Beta BV", DateTime.UtcNow.AddMinutes(-1));

        alphaSubmission.Reviews.Add(CreateReview(
            alphaSubmission.Id,
            firstQuestion.Id,
            TenderReviewRating.Good,
            secondQuestion.Id,
            TenderReviewRating.Excellent));
        betaSubmission.Reviews.Add(CreateReview(
            betaSubmission.Id,
            firstQuestion.Id,
            TenderReviewRating.Sufficient,
            secondQuestion.Id,
            TenderReviewRating.Good));

        var tender = CreateTender(
            organisationId: organisationId,
            status: TenderStatus.Completed,
            questions: [firstQuestion, secondQuestion]);
        tender.Submissions.AddRange([alphaSubmission, betaSubmission]);
        tender.Reviewers.Add(new TenderReviewer("reviewer-1"));

        tenderRepository
            .Setup(repository => repository.GetByIdWithComparisonDataAsync(tender.Id))
            .ReturnsAsync(tender);
        SetupCurrentUser(Roles.Inkoper, organisationId);

        var service = CreateTenderComparisonService();

        // Act
        var dashboard = await service.GetTenderComparisonDashboardAsync(tender.Id, UserId);

        // Assert
        Assert.Equal(tender.Id, dashboard.TenderId);
        Assert.Equal(TenderStatus.Completed, dashboard.Status);
        Assert.Equal(30, dashboard.MaximumScore);
        Assert.Equal(1, dashboard.ReviewerCount);
        Assert.Collection(
            dashboard.Submissions,
            submission =>
            {
                Assert.Equal(alphaSubmission.Id, submission.SubmissionId);
                Assert.Equal(1, submission.Rank);
                Assert.Equal(28, submission.AwardedScore);
                Assert.Equal(93.33m, submission.ScorePercentage);
                Assert.Equal(1, submission.CompletedReviewCount);
                Assert.Equal(1, submission.ReviewerCount);
            },
            submission =>
            {
                Assert.Equal(betaSubmission.Id, submission.SubmissionId);
                Assert.Equal(2, submission.Rank);
                Assert.Equal(22, submission.AwardedScore);
                Assert.Equal(73.33m, submission.ScorePercentage);
                Assert.Equal(1, submission.CompletedReviewCount);
                Assert.Equal(1, submission.ReviewerCount);
            });
    }

    private TenderComparisonService CreateTenderComparisonService()
    {
        return new TenderComparisonService(tenderRepository.Object, currentUserService.Object);
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
        TenderStatus status = TenderStatus.Completed,
        List<TenderQuestion>? questions = null)
    {
        return new Tender
        {
            Id = Guid.NewGuid(),
            Title = "Tender",
            Description = "Description",
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
            Status = status,
            IsPublic = false,
            OrganisationId = organisationId ?? Guid.NewGuid(),
            Questions = questions ?? []
        };
    }

    private static TextQuestion CreateScoredQuestion(int order, int score)
    {
        return new TextQuestion
        {
            Id = Guid.NewGuid(),
            TenderId = Guid.NewGuid(),
            Order = order,
            Text = $"Vraag {order + 1}",
            Rows = 1,
            Score = score
        };
    }

    private static TenderSubmission CreateSubmission(string supplierName, DateTime submittedAt)
    {
        return new TenderSubmission
        {
            Id = Guid.NewGuid(),
            TenderId = Guid.NewGuid(),
            SupplierId = Guid.NewGuid(),
            Supplier = new Organisation
            {
                Id = Guid.NewGuid(),
                Name = supplierName,
                KvkNumber = "12345678",
                OrganisationType = OrganisationType.Supplier
            },
            SubmittedAt = submittedAt
        };
    }

    private static TenderSubmissionReview CreateReview(
        Guid submissionId,
        Guid firstQuestionId,
        TenderReviewRating firstRating,
        Guid secondQuestionId,
        TenderReviewRating secondRating)
    {
        var review = new TenderSubmissionReview(submissionId, "reviewer-1");
        review.SetQuestionRating(firstQuestionId, firstRating);
        review.SetQuestionRating(secondQuestionId, secondRating);
        review.MarkReviewed(DateTime.UtcNow);

        return review;
    }
}
