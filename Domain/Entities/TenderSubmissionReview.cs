using System.ComponentModel.DataAnnotations;
using Domain.Enums;
using Domain.Exceptions;

namespace Domain.Entities;

public class TenderSubmissionReview
{
    private TenderSubmissionReview()
    {
    }

    public TenderSubmissionReview(Guid submissionId, string reviewerUserId)
    {
        if (submissionId == Guid.Empty)
            throw new BusinessRuleViolationException("De offerte is ongeldig.");

        if (string.IsNullOrWhiteSpace(reviewerUserId))
            throw new BusinessRuleViolationException("De beoordelaar is ongeldig.");

        SubmissionId = submissionId;
        ReviewerUserId = reviewerUserId;
    }

    public Guid Id { get; private set; }

    public Guid SubmissionId { get; private set; }
    public TenderSubmission? Submission { get; private set; }

    [MaxLength(450)]
    public string ReviewerUserId { get; private set; } = string.Empty;
    public ApplicationUser? Reviewer { get; private set; }

    public DateTime? ReviewedAt { get; private set; }

    public ICollection<TenderQuestionReview> QuestionReviews { get; private set; } = [];

    public void SetQuestionRating(Guid questionId, TenderReviewRating rating)
    {
        if (questionId == Guid.Empty)
            throw new BusinessRuleViolationException("De vraag is ongeldig.");

        var existingReview = QuestionReviews.FirstOrDefault(review => review.QuestionId == questionId);

        if (existingReview is null)
        {
            QuestionReviews.Add(new TenderQuestionReview(questionId, rating));
            return;
        }

        existingReview.UpdateRating(rating);
    }

    public void MarkReviewed(DateTime reviewedAt)
    {
        if (reviewedAt == default)
            throw new BusinessRuleViolationException("Het beoordelingsmoment is ongeldig.");

        ReviewedAt = reviewedAt;
    }
}
