using Domain.Entities.TenderQuestions;
using Domain.Enums;
using Domain.Exceptions;

namespace Domain.Entities;

public class TenderQuestionReview
{
    private TenderQuestionReview()
    {
    }

    public TenderQuestionReview(Guid questionId, TenderReviewRating rating)
    {
        if (questionId == Guid.Empty)
            throw new BusinessRuleViolationException("De vraag is ongeldig.");

        QuestionId = questionId;
        UpdateRating(rating);
    }

    public Guid Id { get; private set; }

    public Guid SubmissionReviewId { get; private set; }
    public TenderSubmissionReview? SubmissionReview { get; private set; }

    public Guid QuestionId { get; private set; }
    public TenderQuestion? Question { get; private set; }

    public TenderReviewRating Rating { get; private set; }

    public void UpdateRating(TenderReviewRating rating)
    {
        if (!Enum.IsDefined(rating))
            throw new BusinessRuleViolationException("De beoordeling is ongeldig.");

        Rating = rating;
    }
}
