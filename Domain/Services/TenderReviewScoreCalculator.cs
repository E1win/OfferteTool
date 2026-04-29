using Domain.Entities;
using Domain.Entities.TenderQuestions;
using Domain.Enums;
using Domain.Exceptions;

namespace Domain.Services;

public static class TenderReviewScoreCalculator
{
    private const int AwardedPointsPrecision = 2;

    public static decimal GetRatingPercentage(TenderReviewRating rating) =>
        rating switch
        {
            TenderReviewRating.Excellent => 1.00m,
            TenderReviewRating.Good => 0.80m,
            TenderReviewRating.Sufficient => 0.60m,
            TenderReviewRating.Insufficient => 0.40m,
            TenderReviewRating.Poor => 0.20m,
            _ => throw new BusinessRuleViolationException("De beoordeling is ongeldig.")
        };

    public static decimal CalculateAwardedPoints(TenderQuestion question, TenderReviewRating rating)
    {
        ArgumentNullException.ThrowIfNull(question);

        if (!question.Score.HasValue)
            throw new BusinessRuleViolationException("Alleen vragen met een score kunnen worden beoordeeld.");

        return RoundAwardedPoints(question.Score.Value * GetRatingPercentage(rating));
    }

    public static decimal CalculateAverageAwardedPoints(
        TenderQuestion question,
        IEnumerable<TenderSubmissionReview> reviews)
    {
        ArgumentNullException.ThrowIfNull(question);
        ArgumentNullException.ThrowIfNull(reviews);

        if (!question.Score.HasValue)
            throw new BusinessRuleViolationException("Alleen vragen met een score kunnen worden beoordeeld.");

        var ratings = reviews
            .SelectMany(review => review.QuestionReviews)
            .Where(questionReview => questionReview.QuestionId == question.Id)
            .Select(questionReview => questionReview.Rating)
            .ToList();

        if (ratings.Count == 0)
            return 0;

        var averagePercentage = ratings.Average(GetRatingPercentage);
        return RoundAwardedPoints(question.Score.Value * averagePercentage);
    }

    public static decimal CalculateMaximumPoints(IEnumerable<TenderQuestion> questions)
    {
        ArgumentNullException.ThrowIfNull(questions);

        return questions
            .Where(question => question.Score.HasValue)
            .Sum(question => question.Score!.Value);
    }

    public static decimal CalculateAwardedPoints(
        TenderSubmissionReview review,
        IEnumerable<TenderQuestion> questions)
    {
        ArgumentNullException.ThrowIfNull(review);
        ArgumentNullException.ThrowIfNull(questions);

        var questionsById = questions
            .Where(question => question.Score.HasValue)
            .ToDictionary(question => question.Id);

        decimal totalAwardedPoints = 0;

        foreach (var questionReview in review.QuestionReviews)
        {
            if (!questionsById.TryGetValue(questionReview.QuestionId, out var question))
                throw new BusinessRuleViolationException("Een beoordeling kan alleen worden gekoppeld aan een vraag met score uit hetzelfde offertetraject.");

            totalAwardedPoints += CalculateAwardedPoints(question, questionReview.Rating);
        }

        return totalAwardedPoints;
    }

    public static decimal CalculateAwardedPoints(
        IEnumerable<TenderSubmissionReview> reviews,
        IEnumerable<TenderQuestion> questions)
    {
        ArgumentNullException.ThrowIfNull(reviews);
        ArgumentNullException.ThrowIfNull(questions);

        var scoredQuestions = questions
            .Where(question => question.Score.HasValue)
            .ToList();

        ValidateScoredQuestionReviews(reviews, scoredQuestions);

        decimal totalAwardedPoints = 0;

        foreach (var question in scoredQuestions)
            totalAwardedPoints += CalculateAverageAwardedPoints(question, reviews);

        return totalAwardedPoints;
    }

    private static decimal RoundAwardedPoints(decimal awardedPoints) =>
        decimal.Round(
            awardedPoints,
            AwardedPointsPrecision,
            MidpointRounding.AwayFromZero);

    private static void ValidateScoredQuestionReviews(
        IEnumerable<TenderSubmissionReview> reviews,
        IEnumerable<TenderQuestion> scoredQuestions)
    {
        var scoredQuestionIds = scoredQuestions
            .Select(question => question.Id)
            .ToHashSet();

        foreach (var questionReview in reviews.SelectMany(review => review.QuestionReviews))
        {
            if (!scoredQuestionIds.Contains(questionReview.QuestionId))
                throw new BusinessRuleViolationException("Een beoordeling kan alleen worden gekoppeld aan een vraag met score uit hetzelfde offertetraject.");
        }
    }
}
