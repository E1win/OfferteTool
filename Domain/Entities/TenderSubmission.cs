using Domain.Entities.TenderAnswers;
using Domain.Entities.TenderQuestions;
using Domain.Exceptions;

namespace Domain.Entities;

public class TenderSubmission
{
    public Guid Id { get; set; }

    public Guid TenderId { get; set; }
    public Tender? Tender { get; set; }

    public Guid SupplierId { get; set; }
    public Organisation? Supplier { get; set; }

    public DateTime SubmittedAt { get; set; }

    public ICollection<TenderAnswer> Answers { get; set; } = [];
    public ICollection<TenderSubmissionReview> Reviews { get; set; } = [];

    public void Submit(Tender tender, IEnumerable<TenderAnswer> answers, DateTime submittedAt)
    {
        ArgumentNullException.ThrowIfNull(tender);
        ArgumentNullException.ThrowIfNull(answers);

        if (tender.Id != TenderId)
            throw new BusinessRuleViolationException("Deze inschrijving hoort niet bij het opgegeven offertetraject.");

        var tenderQuestions = tender.Questions
            .ToDictionary(question => question.Id);

        var submittedAnswers = answers.ToList();

        if (submittedAnswers.Count != tenderQuestions.Count)
            throw new BusinessRuleViolationException("Beantwoord alle vragen voordat u de inschrijving verzendt.");

        var duplicateAnswer = submittedAnswers
            .GroupBy(answer => answer.QuestionId)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateAnswer is not null)
            throw new BusinessRuleViolationException("Elke vraag mag maar één keer worden beantwoord.");

        foreach (var answer in submittedAnswers)
        {
            if (!tenderQuestions.TryGetValue(answer.QuestionId, out var question))
                throw new BusinessRuleViolationException("Een antwoord kan alleen worden gekoppeld aan een vraag uit hetzelfde offertetraject.");

            EnsureAnswerBelongsToTender(answer, question);
            question.ValidateAnswer(answer);
            answer.Question = question;
        }

        Answers = submittedAnswers;
        SubmittedAt = submittedAt;
    }

    private void EnsureAnswerBelongsToTender(TenderAnswer answer, TenderQuestion question)
    {
        ArgumentNullException.ThrowIfNull(answer);
        ArgumentNullException.ThrowIfNull(question);

        if (question.TenderId != TenderId)
            throw new BusinessRuleViolationException("Een antwoord kan alleen worden gekoppeld aan een vraag uit hetzelfde offertetraject.");

        if (answer.QuestionId != question.Id)
            throw new BusinessRuleViolationException("Het antwoord hoort niet bij de opgegeven vraag.");
    }
}
