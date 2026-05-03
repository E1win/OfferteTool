using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Models.TenderComparison;
using Domain.Entities;
using Domain.Entities.TenderAnswers;
using Domain.Entities.TenderQuestions;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Services;

namespace Application.Services;

public class TenderComparisonService(
    ITenderRepository tenderRepository,
    ITenderSubmissionRepository tenderSubmissionRepository,
    ICurrentUserService currentUserService,
    ITenderSubmissionEncryptionService tenderSubmissionEncryptionService) : ITenderComparisonService
{
    public async Task<TenderComparisonDashboard> GetTenderComparisonDashboardAsync(Guid tenderId, string userId)
    {
        var tender = await tenderRepository.GetByIdWithComparisonDataAsync(tenderId)
            ?? throw new KeyNotFoundException("Dit offertetraject kon niet worden gevonden.");

        var (user, role) = await currentUserService.GetUserWithRoleAsync(userId);

        if (!tender.CanBeManagedBy(user, role))
            throw new UnauthorizedAccessException("U kunt dit offertetraject niet beheren.");

        if (tender.Status != TenderStatus.Completed)
            throw new BusinessRuleViolationException("Inschrijvingen kunnen pas worden vergeleken zodra het offertetraject is afgerond.");

        var scoredQuestions = tender.Questions
            .Where(question => question.Score.HasValue)
            .OrderBy(question => question.Order)
            .ToList();
        var maximumScore = TenderReviewScoreCalculator.CalculateMaximumPoints(scoredQuestions);
        var submissions = tender.Submissions
            .OrderBy(submission => submission.Supplier?.Name ?? string.Empty)
            .ThenBy(submission => submission.SubmittedAt)
            .ToList();
        var submissionRows = CreateSubmissionRows(submissions, scoredQuestions, maximumScore, tender.Reviewers.Count);

        return new TenderComparisonDashboard
        {
            TenderId = tender.Id,
            TenderTitle = tender.Title,
            TenderDescription = tender.Description,
            Status = tender.Status,
            MaximumScore = maximumScore,
            ReviewerCount = tender.Reviewers.Count,
            Submissions = submissionRows
        };
    }

    public async Task<TenderSubmissionComparisonDetails> GetTenderSubmissionComparisonDetailsAsync(
        Guid tenderId,
        Guid submissionId,
        string userId)
    {
        var submission = await tenderSubmissionRepository.GetComparisonDetailsAsync(submissionId)
            ?? throw new KeyNotFoundException("De inschrijving kon niet worden gevonden.");

        if (submission.TenderId != tenderId)
            throw new KeyNotFoundException("De inschrijving kon niet worden gevonden.");

        var tender = submission.Tender
            ?? throw new InvalidOperationException("De inschrijving is niet volledig geladen voor vergelijking.");

        var (user, role) = await currentUserService.GetUserWithRoleAsync(userId);

        if (!tender.CanBeManagedBy(user, role))
            throw new UnauthorizedAccessException("U kunt dit offertetraject niet beheren.");

        if (tender.Status != TenderStatus.Completed)
            throw new BusinessRuleViolationException("Inschrijvingen kunnen pas worden vergeleken zodra het offertetraject is afgerond.");

        tenderSubmissionEncryptionService.Decrypt(submission);

        var questions = tender.Questions
            .OrderBy(question => question.Order)
            .ToList();
        var scoredQuestions = questions
            .Where(question => question.Score.HasValue)
            .ToList();
        var maximumScore = TenderReviewScoreCalculator.CalculateMaximumPoints(scoredQuestions);
        var awardedScore = TenderReviewScoreCalculator.CalculateAwardedPoints(submission.Reviews, scoredQuestions);

        return new TenderSubmissionComparisonDetails
        {
            TenderId = tender.Id,
            TenderTitle = tender.Title,
            TenderDescription = tender.Description,
            SubmissionId = submission.Id,
            SupplierName = GetSupplierName(submission),
            SubmittedAt = submission.SubmittedAt,
            MaximumScore = maximumScore,
            AwardedScore = awardedScore,
            ScorePercentage = CalculatePercentage(awardedScore, maximumScore),
            CompletedReviewCount = submission.Reviews.Count,
            ReviewerCount = tender.Reviewers.Count,
            Questions = CreateQuestionDetails(questions, submission)
        };
    }

    private static List<TenderComparisonSubmission> CreateSubmissionRows(
        IReadOnlyList<TenderSubmission> submissions,
        IReadOnlyList<TenderQuestion> scoredQuestions,
        decimal maximumScore,
        int reviewerCount)
    {
        var rankedSubmissions = submissions
            .Select(submission => new
            {
                Submission = submission,
                AwardedScore = TenderReviewScoreCalculator.CalculateAwardedPoints(submission.Reviews, scoredQuestions)
            })
            .OrderByDescending(submission => submission.AwardedScore)
            .ThenBy(submission => submission.Submission.Supplier?.Name ?? string.Empty)
            .ThenBy(submission => submission.Submission.SubmittedAt)
            .ToList();

        var rows = new List<TenderComparisonSubmission>();
        decimal? previousScore = null;
        var previousRank = 0;

        for (var index = 0; index < rankedSubmissions.Count; index++)
        {
            var rankedSubmission = rankedSubmissions[index];
            var rank = previousScore == rankedSubmission.AwardedScore
                ? previousRank
                : index + 1;

            rows.Add(new TenderComparisonSubmission
            {
                SubmissionId = rankedSubmission.Submission.Id,
                SupplierName = GetSupplierName(rankedSubmission.Submission),
                SubmittedAt = rankedSubmission.Submission.SubmittedAt,
                Rank = rank,
                AwardedScore = rankedSubmission.AwardedScore,
                ScorePercentage = CalculatePercentage(rankedSubmission.AwardedScore, maximumScore),
                CompletedReviewCount = rankedSubmission.Submission.Reviews.Count,
                ReviewerCount = reviewerCount
            });

            previousScore = rankedSubmission.AwardedScore;
            previousRank = rank;
        }

        return rows;
    }

    private static decimal CalculatePercentage(decimal awardedScore, decimal maximumScore) =>
        maximumScore == 0
            ? 0
            : decimal.Round(awardedScore / maximumScore * 100, 2, MidpointRounding.AwayFromZero);

    private static string GetSupplierName(TenderSubmission submission) =>
        submission.Supplier?.Name ?? "Onbekende leverancier";

    private static List<TenderSubmissionComparisonQuestion> CreateQuestionDetails(
        IReadOnlyList<TenderQuestion> questions,
        TenderSubmission submission)
    {
        var answersByQuestionId = submission.Answers
            .ToDictionary(answer => answer.QuestionId);

        return questions
            .Select(question =>
            {
                if (!answersByQuestionId.TryGetValue(question.Id, out var answer))
                    throw new InvalidOperationException("De inschrijving bevat niet voor elke vraag een antwoord.");

                return new TenderSubmissionComparisonQuestion
                {
                    QuestionId = question.Id,
                    Order = question.Order,
                    Text = question.Text,
                    MaximumScore = question.Score,
                    Answer = CreateAnswer(question, answer),
                    ReviewerScores = CreateReviewerScores(question, submission.Reviews)
                };
            })
            .ToList();
    }

    private static TenderSubmissionComparisonAnswer CreateAnswer(TenderQuestion question, TenderAnswer answer)
    {
        return (question, answer) switch
        {
            (TextQuestion, TextAnswer textAnswer) => new TenderSubmissionComparisonAnswer
            {
                Type = answer.Type,
                TextValue = textAnswer.TextValue,
                SelectedOptions = []
            },
            (NumberQuestion, NumberAnswer numberAnswer) => new TenderSubmissionComparisonAnswer
            {
                Type = answer.Type,
                NumericValue = numberAnswer.NumericValue,
                SelectedOptions = []
            },
            (ChoiceQuestion choiceQuestion, ChoiceAnswer choiceAnswer) => new TenderSubmissionComparisonAnswer
            {
                Type = answer.Type,
                SelectedOptions = CreateSelectedOptionLabels(choiceQuestion, choiceAnswer)
            },
            _ => throw new InvalidOperationException("Het opgeslagen antwoordtype past niet bij de vraag.")
        };
    }

    private static IReadOnlyList<string> CreateSelectedOptionLabels(ChoiceQuestion question, ChoiceAnswer answer)
    {
        var optionsById = question.Options.ToDictionary(option => option.Id);

        return answer.Selections
            .Select(selection => optionsById.TryGetValue(selection.OptionId, out var option)
                ? option.Text
                : "Onbekende keuze")
            .ToList();
    }

    private static List<TenderSubmissionComparisonReviewerScore> CreateReviewerScores(
        TenderQuestion question,
        IEnumerable<TenderSubmissionReview> reviews)
    {
        if (!question.Score.HasValue)
            return [];

        return reviews
            .Select(review => new
            {
                Review = review,
                QuestionReview = review.QuestionReviews.FirstOrDefault(questionReview => questionReview.QuestionId == question.Id)
            })
            .Where(review => review.QuestionReview is not null)
            .OrderBy(review => GetReviewerName(review.Review))
            .Select(review => new TenderSubmissionComparisonReviewerScore
            {
                ReviewerName = GetReviewerName(review.Review),
                Rating = review.QuestionReview!.Rating,
                AwardedScore = TenderReviewScoreCalculator.CalculateAwardedPoints(question, review.QuestionReview.Rating)
            })
            .ToList();
    }

    private static string GetReviewerName(TenderSubmissionReview review)
    {
        var reviewer = review.Reviewer;

        if (reviewer is null)
            return "Onbekende beoordelaar";

        var fullName = $"{reviewer.FirstName} {reviewer.LastName}".Trim();
        if (!string.IsNullOrWhiteSpace(fullName) && !string.IsNullOrWhiteSpace(reviewer.Email))
            return $"{fullName} ({reviewer.Email})";
        if (!string.IsNullOrWhiteSpace(fullName))
            return fullName;
        if (!string.IsNullOrWhiteSpace(reviewer.Email))
            return reviewer.Email!;

        return reviewer.UserName ?? "Onbekende beoordelaar";
    }
}
