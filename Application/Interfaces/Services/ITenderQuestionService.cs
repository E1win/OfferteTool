using Application.Models.Questionnaire;
using Domain.Entities.TenderQuestions;

namespace Application.Interfaces.Services;

public interface ITenderQuestionService
{
    Task<List<TenderQuestion>> GetQuestionsAsync(Guid tenderId, string userId);
    Task<TenderQuestion> CreateQuestionAsync(Guid tenderId, TenderQuestion question, string userId);
    Task<TenderQuestion> UpdateQuestionAsync(Guid tenderId, Guid questionId, TenderQuestion updatedQuestion, string userId);
    Task<TenderQuestion> AmendQuestionTextAsync(Guid tenderId, Guid questionId, QuestionTextAmendment amendment, string userId);
    Task DeleteQuestionAsync(Guid tenderId, Guid questionId, string userId);
    Task ReorderQuestionsAsync(Guid tenderId, List<Guid> orderedQuestionIds, string userId);
}
