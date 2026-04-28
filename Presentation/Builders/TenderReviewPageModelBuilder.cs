using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Entities.TenderAnswers;
using Domain.Entities.TenderQuestions;
using Domain.Enums;
using Presentation.Models.TenderReview;

namespace Presentation.Builders;

public class TenderReviewPageModelBuilder(ITenderReviewService tenderReviewService) : ITenderReviewPageModelBuilder
{
    private static readonly IReadOnlyList<TenderReviewRatingOptionViewModel> RatingOptions =
    [
        new() { Value = TenderReviewRating.Poor, Label = "Slecht" },
        new() { Value = TenderReviewRating.Insufficient, Label = "Onvoldoende" },
        new() { Value = TenderReviewRating.Sufficient, Label = "Voldoende" },
        new() { Value = TenderReviewRating.Good, Label = "Goed" },
        new() { Value = TenderReviewRating.Excellent, Label = "Uitstekend" },
    ];

    public async Task<TenderReviewPageViewModel> BuildEditAsync(
        Guid tenderId,
        Guid submissionId,
        string reviewerUserId,
        TenderReviewFormViewModel? form = null,
        string? errorMessage = null)
    {
        var submission = await tenderReviewService.GetSubmissionForReviewAsync(tenderId, submissionId, reviewerUserId);
        var review = await tenderReviewService.GetReviewAsync(tenderId, submissionId, reviewerUserId);
        var tender = submission.Tender
            ?? throw new InvalidOperationException("De inschrijving is niet volledig geladen voor beoordeling.");

        var questions = tender.Questions
            .OrderBy(question => question.Order)
            .ToList();
        var normalizedForm = CreateReviewForm(questions, form, review);

        return new TenderReviewPageViewModel
        {
            TenderId = tender.Id,
            TenderTitle = tender.Title,
            TenderDescription = tender.Description,
            SubmissionId = submission.Id,
            SupplierName = submission.Supplier?.Name ?? "Onbekende leverancier",
            SubmittedAt = submission.SubmittedAt,
            Questions = CreateQuestionViewModels(questions, submission, normalizedForm),
            Form = normalizedForm,
            RatingOptions = RatingOptions,
            ErrorMessage = errorMessage
        };
    }

    private static TenderReviewFormViewModel CreateReviewForm(
        IReadOnlyList<TenderQuestion> questions,
        TenderReviewFormViewModel? postedForm,
        TenderSubmissionReview? review)
    {
        var postedRatings = postedForm?.Ratings
            .GroupBy(rating => rating.QuestionId)
            .ToDictionary(group => group.Key, group => group.First().Rating);

        var persistedRatings = review?.QuestionReviews
            .GroupBy(questionReview => questionReview.QuestionId)
            .ToDictionary(group => group.Key, group => (TenderReviewRating?)group.First().Rating)
            ?? [];

        return new TenderReviewFormViewModel
        {
            Ratings = questions
                .Where(question => question.Score.HasValue)
                .Select(question => new TenderQuestionReviewInputModel
                {
                    QuestionId = question.Id,
                    Rating = postedRatings is not null && postedRatings.TryGetValue(question.Id, out var postedRating)
                        ? postedRating
                        : persistedRatings.GetValueOrDefault(question.Id)
                })
                .ToList()
        };
    }

    private static IReadOnlyList<TenderReviewQuestionViewModel> CreateQuestionViewModels(
        IReadOnlyList<TenderQuestion> questions,
        TenderSubmission submission,
        TenderReviewFormViewModel form)
    {
        var answersByQuestionId = submission.Answers
            .ToDictionary(answer => answer.QuestionId);
        var ratingIndexesByQuestionId = form.Ratings
            .Select((rating, index) => new { rating.QuestionId, Index = index })
            .ToDictionary(rating => rating.QuestionId, rating => rating.Index);

        return questions
            .Select((question, index) => CreateQuestionViewModel(
                question,
                answersByQuestionId[question.Id],
                index,
                ratingIndexesByQuestionId.GetValueOrDefault(question.Id, -1)))
            .ToList();
    }

    private static TenderReviewQuestionViewModel CreateQuestionViewModel(
        TenderQuestion question,
        TenderAnswer answer,
        int index,
        int ratingIndex)
    {
        var ratingInputIndex = ratingIndex >= 0 ? ratingIndex : (int?)null;

        return (question, answer) switch
        {
            (TextQuestion, TextAnswer textAnswer) => new TextTenderReviewQuestionViewModel
            {
                Index = index,
                QuestionId = question.Id,
                Text = question.Text,
                Score = question.Score,
                RatingInputIndex = ratingInputIndex,
                Answer = textAnswer.TextValue ?? string.Empty
            },
            (NumberQuestion, NumberAnswer numberAnswer) => new NumberTenderReviewQuestionViewModel
            {
                Index = index,
                QuestionId = question.Id,
                Text = question.Text,
                Score = question.Score,
                RatingInputIndex = ratingInputIndex,
                Answer = numberAnswer.NumericValue
            },
            (ChoiceQuestion choiceQuestion, ChoiceAnswer choiceAnswer) => new ChoiceTenderReviewQuestionViewModel
            {
                Index = index,
                QuestionId = question.Id,
                Text = question.Text,
                Score = question.Score,
                RatingInputIndex = ratingInputIndex,
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
}
