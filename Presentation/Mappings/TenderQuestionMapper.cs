using Domain.Entities.TenderQuestions;
using Domain.Enums;
using Presentation.Models.Questionnaire;

namespace Presentation.Mappings;

public static class TenderQuestionMapper
{
    public static TenderQuestion ToEntity(QuestionnaireQuestionInputModel model)
    {
        return model.Type switch
        {
            QuestionType.Text => new TextQuestion
            {
                TenderId = Guid.Empty,
                Text = model.Text,
                Score = model.Score,
                Rows = model.Rows ?? 1,
                MaxLength = model.MaxLength
            },
            QuestionType.Numeric => new NumberQuestion
            {
                TenderId = Guid.Empty,
                Text = model.Text,
                Score = model.Score,
                MinValue = model.MinValue,
                MaxValue = model.MaxValue
            },
            QuestionType.Choice => new ChoiceQuestion
            {
                TenderId = Guid.Empty,
                Text = model.Text,
                Score = model.Score,
                AllowMultipleSelection = model.AllowMultipleSelection,
                Options = model.Options
                    .Select((option, index) => new TenderQuestionOption
                    {
                        Id = option.Id ?? Guid.Empty,
                        Order = index,
                        Text = option.Text
                    })
                    .ToList()
            },
            _ => throw new InvalidOperationException("Het gekozen vraagtype is ongeldig.")
        };
    }

    public static QuestionnaireQuestionViewModel ToViewModel(TenderQuestion question)
    {
        return question switch
        {
            ChoiceQuestion choiceQuestion => new QuestionnaireQuestionViewModel
            {
                Id = choiceQuestion.Id,
                Text = choiceQuestion.Text,
                Score = choiceQuestion.Score,
                Type = choiceQuestion.Type,
                TypeLabel = choiceQuestion.AllowMultipleSelection ? "Meerkeuze" : "Enkele keuze",
                Order = choiceQuestion.Order,
                AllowMultipleSelection = choiceQuestion.AllowMultipleSelection,
                Rows = null,
                MaxLength = null,
                MinValue = null,
                MaxValue = null,
                Options = choiceQuestion.Options
                    .OrderBy(option => option.Order)
                    .Select(option => new QuestionnaireOptionViewModel
                    {
                        Id = option.Id,
                        Text = option.Text,
                        Order = option.Order
                    })
                    .ToList()
            },
            NumberQuestion numberQuestion => new QuestionnaireQuestionViewModel
            {
                Id = numberQuestion.Id,
                Text = numberQuestion.Text,
                Score = numberQuestion.Score,
                Type = numberQuestion.Type,
                TypeLabel = "Numeriek",
                Order = numberQuestion.Order,
                AllowMultipleSelection = false,
                Rows = null,
                MaxLength = null,
                MinValue = numberQuestion.MinValue,
                MaxValue = numberQuestion.MaxValue,
                Options = []
            },
            TextQuestion textQuestion => new QuestionnaireQuestionViewModel
            {
                Id = textQuestion.Id,
                Text = textQuestion.Text,
                Score = textQuestion.Score,
                Type = textQuestion.Type,
                TypeLabel = "Tekst",
                Order = textQuestion.Order,
                AllowMultipleSelection = false,
                Rows = textQuestion.Rows,
                MaxLength = textQuestion.MaxLength,
                MinValue = null,
                MaxValue = null,
                Options = []
            },
            _ => throw new InvalidOperationException("Het vraagtype wordt niet ondersteund.")
        };
    }
}