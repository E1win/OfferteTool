using Domain.Entities;
using Domain.Entities.TenderAnswers;
using Domain.Entities.TenderQuestions;
using Domain.Enums;
using Infrastructure.Configuration;
using Infrastructure.Data;
using Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tests.E2E.TestData;

namespace Tests.E2E.Seeding;

public sealed class TenderScenarioSeeder
{
    private const string InkoperEmail = "inkoper@test.nl";
    private const string BeoordelaarEmail = "beoordelaar@test.nl";
    private const string LeverancierEmail = "leverancier@test.nl";
    private const string ReviewerName = "Pieter Bakker (beoordelaar@test.nl)";
    private const string SupplierName = "Leverancier B.V.";
    private const int TextQuestionScore = 30;
    private const int NumericQuestionScore = 20;
    private const int ChoiceQuestionScore = 50;
    private static readonly Lazy<IServiceProvider> DefaultServices = new(E2ESeederServices.Create);

    private readonly IServiceProvider services;

    public TenderScenarioSeeder()
        : this(DefaultServices.Value)
    {
    }

    public TenderScenarioSeeder(IServiceProvider services)
    {
        this.services = services;
    }

    public async Task<TenderScenario> CreateClosedTenderWithSubmittedOfferAsync(TenderDraft tender)
    {
        var scenario = CreateScenario(tender);

        await SeedScenarioAsync(scenario, TenderStatus.Closed, includeReview: false);

        return scenario;
    }

    public async Task<TenderScenario> CreateCompletedTenderWithReviewedSubmissionAsync(TenderDraft tender)
    {
        var scenario = CreateScenario(tender);

        await SeedScenarioAsync(scenario, TenderStatus.Completed, includeReview: true);

        return scenario;
    }

    private TenderScenario CreateScenario(TenderDraft tender)
    {
        var choiceOptionId = Guid.NewGuid();

        return new TenderScenario(
            TenderId: Guid.NewGuid(),
            SubmissionId: Guid.NewGuid(),
            Tender: tender,
            Questions: new TenderScenarioQuestions(
                TextQuestionId: Guid.NewGuid(),
                TextQuestion: $"{tender.Title} tekstvraag",
                NumericQuestionId: Guid.NewGuid(),
                NumericQuestion: $"{tender.Title} numerieke vraag",
                ChoiceQuestionId: Guid.NewGuid(),
                ChoiceQuestion: $"{tender.Title} keuzevraag",
                ChoiceOptionId: choiceOptionId,
                ChoiceOption: "Ja"),
            Answers: new TenderScenarioAnswers(
                TextAnswer: "Wij kunnen deze opdracht volledig uitvoeren.",
                NumericAnswer: "42",
                ChoiceAnswer: "Ja"),
            ReviewerName: ReviewerName,
            SupplierName: SupplierName);
    }

    private async Task SeedScenarioAsync(TenderScenario scenario, TenderStatus finalStatus, bool includeReview)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var encryptionService = scope.ServiceProvider.GetRequiredService<AesTenderSubmissionEncryptionService>();

        var inkoper = await GetUserByEmailAsync(dbContext, InkoperEmail);
        var beoordelaar = await GetUserByEmailAsync(dbContext, BeoordelaarEmail);
        var leverancier = await GetUserByEmailAsync(dbContext, LeverancierEmail);
        var clientOrganisationId = inkoper.OrganisationId
            ?? throw new InvalidOperationException("De inkoper is niet gekoppeld aan een opdrachtgeverorganisatie.");
        var supplierOrganisationId = leverancier.OrganisationId
            ?? throw new InvalidOperationException("De leverancier is niet gekoppeld aan een leveranciersorganisatie.");

        var tender = CreateTender(scenario, clientOrganisationId);
        tender.AddReviewer(inkoper.Id);
        tender.AddReviewer(beoordelaar.Id);

        dbContext.Tenders.Add(tender);

        var submission = CreateSubmission(scenario, tender, supplierOrganisationId);
        encryptionService.Encrypt(submission);
        dbContext.TenderSubmissions.Add(submission);

        if (includeReview)
            dbContext.TenderSubmissionReviews.Add(CreateReview(scenario, beoordelaar.Id));

        tender.Status = finalStatus;

        await dbContext.SaveChangesAsync();
    }

    private static Tender CreateTender(TenderScenario scenario, Guid clientOrganisationId)
    {
        var tender = new Tender
        {
            Id = scenario.TenderId,
            Title = scenario.Tender.Title,
            Description = scenario.Tender.Description,
            EndDate = scenario.Tender.EndDate,
            Status = TenderStatus.Closed,
            IsPublic = scenario.Tender.IsPublic,
            OrganisationId = clientOrganisationId
        };

        tender.Questions.AddRange(
        [
            new TextQuestion
            {
                Id = scenario.Questions.TextQuestionId,
                TenderId = tender.Id,
                Order = 0,
                Text = scenario.Questions.TextQuestion,
                Score = TextQuestionScore,
                Rows = 4,
                MaxLength = 500
            },
            new NumberQuestion
            {
                Id = scenario.Questions.NumericQuestionId,
                TenderId = tender.Id,
                Order = 1,
                Text = scenario.Questions.NumericQuestion,
                Score = NumericQuestionScore,
                MinValue = 1,
                MaxValue = 100
            },
            new ChoiceQuestion
            {
                Id = scenario.Questions.ChoiceQuestionId,
                TenderId = tender.Id,
                Order = 2,
                Text = scenario.Questions.ChoiceQuestion,
                Score = ChoiceQuestionScore,
                AllowMultipleSelection = false,
                Options =
                [
                    new TenderQuestionOption
                    {
                        Id = scenario.Questions.ChoiceOptionId,
                        QuestionId = scenario.Questions.ChoiceQuestionId,
                        Order = 0,
                        Text = scenario.Questions.ChoiceOption
                    },
                    new TenderQuestionOption
                    {
                        Id = Guid.NewGuid(),
                        QuestionId = scenario.Questions.ChoiceQuestionId,
                        Order = 1,
                        Text = "Nee"
                    }
                ]
            }
        ]);

        return tender;
    }

    private static TenderSubmission CreateSubmission(TenderScenario scenario, Tender tender, Guid supplierOrganisationId)
    {
        var submission = new TenderSubmission
        {
            Id = scenario.SubmissionId,
            TenderId = tender.Id,
            SupplierId = supplierOrganisationId
        };
        var choiceAnswerId = Guid.NewGuid();

        submission.Submit(
            tender,
            [
                new TextAnswer
                {
                    Id = Guid.NewGuid(),
                    SubmissionId = submission.Id,
                    QuestionId = scenario.Questions.TextQuestionId,
                    Type = AnswerType.Text,
                    TextValue = scenario.Answers.TextAnswer
                },
                new NumberAnswer
                {
                    Id = Guid.NewGuid(),
                    SubmissionId = submission.Id,
                    QuestionId = scenario.Questions.NumericQuestionId,
                    Type = AnswerType.Numeric,
                    NumericValue = decimal.Parse(scenario.Answers.NumericAnswer)
                },
                new ChoiceAnswer
                {
                    Id = choiceAnswerId,
                    SubmissionId = submission.Id,
                    QuestionId = scenario.Questions.ChoiceQuestionId,
                    Type = AnswerType.Choice,
                    Selections =
                    [
                        new ChoiceAnswerSelection
                        {
                            ChoiceAnswerId = choiceAnswerId,
                            OptionId = scenario.Questions.ChoiceOptionId
                        }
                    ]
                }
            ],
            DateTime.UtcNow);

        return submission;
    }

    private static TenderSubmissionReview CreateReview(TenderScenario scenario, string reviewerUserId)
    {
        var review = new TenderSubmissionReview(scenario.SubmissionId, reviewerUserId);

        review.SetQuestionRating(scenario.Questions.TextQuestionId, TenderReviewRating.Good);
        review.SetQuestionRating(scenario.Questions.NumericQuestionId, TenderReviewRating.Sufficient);
        review.SetQuestionRating(scenario.Questions.ChoiceQuestionId, TenderReviewRating.Excellent);
        review.MarkReviewed(DateTime.UtcNow);

        return review;
    }

    private static async Task<ApplicationUser> GetUserByEmailAsync(AppDbContext dbContext, string email)
    {
        return await dbContext.Users
            .FirstOrDefaultAsync(user => user.Email == email)
            ?? throw new InvalidOperationException($"E2E gebruiker '{email}' bestaat niet. Controleer of de E2E database seed draait.");
    }
}

internal static class E2ESeederServices
{
    public static IServiceProvider Create()
    {
        var configuration = E2EConfiguration.Load();
        var services = new ServiceCollection();

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(configuration.ConnectionString));
        services.AddSingleton(Options.Create(new TenderSubmissionEncryptionOptions
        {
            Algorithm = configuration.EncryptionAlgorithm,
            Key = configuration.EncryptionKey
        }));
        services.AddSingleton<TenderAnswerPayloadSerializer>();
        services.AddScoped<AesTenderSubmissionEncryptionService>();

        return services.BuildServiceProvider();
    }
}
