using Domain.Entities.TenderAnswers;
using Domain.Enums;
using Presentation.Models.TenderSubmission;

namespace Presentation.Mappings;

public static class TenderSubmissionMapper
{
    public static TenderSubmissionFormViewModel ToFormViewModel(IEnumerable<TenderAnswer> answers)
    {
        return new TenderSubmissionFormViewModel
        {
            Answers = answers
                .Select(ToInputModel)
                .ToList()
        };
    }

    public static List<TenderAnswer> ToEntities(TenderSubmissionFormViewModel model)
    {
        return model.Answers.Select(ToEntity).ToList();
    }

    private static TenderSubmissionAnswerInputModel ToInputModel(TenderAnswer answer)
    {
        return answer switch
        {
            TextAnswer textAnswer => new TenderSubmissionAnswerInputModel
            {
                QuestionId = textAnswer.QuestionId,
                Type = QuestionType.Text,
                TextValue = textAnswer.TextValue ?? string.Empty
            },
            NumberAnswer numberAnswer => new TenderSubmissionAnswerInputModel
            {
                QuestionId = numberAnswer.QuestionId,
                Type = QuestionType.Numeric,
                NumericValue = numberAnswer.NumericValue
            },
            ChoiceAnswer choiceAnswer => new TenderSubmissionAnswerInputModel
            {
                QuestionId = choiceAnswer.QuestionId,
                Type = QuestionType.Choice,
                SelectedOptionIds = choiceAnswer.Selections
                    .Select(selection => selection.OptionId)
                    .Distinct()
                    .ToList()
            },
            _ => throw new InvalidOperationException("Het opgeslagen antwoordtype wordt niet ondersteund.")
        };
    }

    private static TenderAnswer ToEntity(TenderSubmissionAnswerInputModel answer)
    {
        return answer.Type switch
        {
            QuestionType.Text => new TextAnswer
            {
                SubmissionId = Guid.Empty,
                QuestionId = answer.QuestionId,
                Type = AnswerType.Text,
                TextValue = answer.TextValue
            },
            QuestionType.Numeric => new NumberAnswer
            {
                SubmissionId = Guid.Empty,
                QuestionId = answer.QuestionId,
                Type = AnswerType.Numeric,
                NumericValue = answer.NumericValue
            },
            QuestionType.Choice => new ChoiceAnswer
            {
                SubmissionId = Guid.Empty,
                QuestionId = answer.QuestionId,
                Type = AnswerType.Choice,
                Selections = answer.SelectedOptionIds
                    .Distinct()
                    .Select(optionId => new ChoiceAnswerSelection
                    {
                        ChoiceAnswerId = Guid.Empty,
                        OptionId = optionId
                    })
                    .ToList()
            },
            _ => throw new InvalidOperationException("Het gekozen antwoordtype wordt niet ondersteund.")
        };
    }
}
