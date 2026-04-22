using Domain.Entities.TenderAnswers;
using Domain.Enums;
using Presentation.Models.TenderSubmission;

namespace Presentation.Mappings;

public static class TenderSubmissionMapper
{
    public static List<TenderAnswer> ToEntities(TenderSubmissionFormViewModel model)
    {
        return model.Answers.Select(ToEntity).ToList();
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
