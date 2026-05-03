using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Services;
using Domain.Constants;
using Domain.Entities;
using Domain.Entities.TenderAnswers;
using Domain.Entities.TenderQuestions;
using Domain.Enums;
using Domain.Exceptions;
using Moq;

namespace Tests.Application.Services;

public class TenderComparisonServiceTests
{
    private const string UserId = "user-1";

    private readonly Mock<ITenderRepository> tenderRepository = new();
    private readonly Mock<ITenderSubmissionRepository> tenderSubmissionRepository = new();
    private readonly Mock<ICurrentUserService> currentUserService = new();
    private readonly Mock<ITenderSubmissionEncryptionService> tenderSubmissionEncryptionService = new();

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
        Assert.Equal(tender.Description, dashboard.TenderDescription);
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

    [Fact]
    public async Task GetTenderSubmissionComparisonDetailsAsync_WhenTenderIsCompleted_ReturnsAnswersAndReviewerScores()
    {
        // Arrange
        var organisationId = Guid.NewGuid();
        var scoredQuestion = CreateScoredQuestion(order: 0, score: 10);
        var unscoredQuestion = new NumberQuestion
        {
            Id = Guid.NewGuid(),
            TenderId = Guid.NewGuid(),
            Order = 1,
            Text = "Levertijd",
            Score = null,
            MinValue = 1,
            MaxValue = 90
        };
        var submission = CreateSubmission("Alpha BV", DateTime.UtcNow.AddMinutes(-2));
        submission.TenderId = Guid.NewGuid();
        submission.Tender = CreateTender(
            organisationId: organisationId,
            status: TenderStatus.Completed,
            questions: [scoredQuestion, unscoredQuestion]);
        submission.Tender.Id = submission.TenderId;
        submission.Answers.Add(new TextAnswer
        {
            Id = Guid.NewGuid(),
            SubmissionId = submission.Id,
            QuestionId = scoredQuestion.Id,
            Type = AnswerType.Text,
            TextValue = "Wij leveren ergonomische stoelen."
        });
        submission.Answers.Add(new NumberAnswer
        {
            Id = Guid.NewGuid(),
            SubmissionId = submission.Id,
            QuestionId = unscoredQuestion.Id,
            Type = AnswerType.Numeric,
            NumericValue = 14
        });
        var review = new TenderSubmissionReview(submission.Id, "reviewer-1");
        review.SetQuestionRating(scoredQuestion.Id, TenderReviewRating.Good);
        review.MarkReviewed(DateTime.UtcNow);
        submission.Reviews.Add(review);

        tenderSubmissionRepository
            .Setup(repository => repository.GetComparisonDetailsAsync(submission.Id))
            .ReturnsAsync(submission);
        SetupCurrentUser(Roles.Inkoper, organisationId);

        var service = CreateTenderComparisonService();

        // Act
        var details = await service.GetTenderSubmissionComparisonDetailsAsync(submission.TenderId, submission.Id, UserId);

        // Assert
        Assert.Equal(submission.TenderId, details.TenderId);
        Assert.Equal(submission.Id, details.SubmissionId);
        Assert.Equal("Alpha BV", details.SupplierName);
        Assert.Equal(10, details.MaximumScore);
        Assert.Equal(8, details.AwardedScore);
        tenderSubmissionEncryptionService.Verify(service => service.Decrypt(submission), Times.Once);
        Assert.Collection(
            details.Questions,
            question =>
            {
                Assert.Equal(scoredQuestion.Id, question.QuestionId);
                Assert.Equal(10, question.MaximumScore);
                Assert.Equal("Wij leveren ergonomische stoelen.", question.Answer.TextValue);
                var reviewerScore = Assert.Single(question.ReviewerScores);
                Assert.Equal(TenderReviewRating.Good, reviewerScore.Rating);
                Assert.Equal(8, reviewerScore.AwardedScore);
            },
            question =>
            {
                Assert.Equal(unscoredQuestion.Id, question.QuestionId);
                Assert.Null(question.MaximumScore);
                Assert.Equal(14, question.Answer.NumericValue);
                Assert.Empty(question.ReviewerScores);
            });
    }

    private TenderComparisonService CreateTenderComparisonService()
    {
        return new TenderComparisonService(
            tenderRepository.Object,
            tenderSubmissionRepository.Object,
            currentUserService.Object,
            tenderSubmissionEncryptionService.Object);
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
