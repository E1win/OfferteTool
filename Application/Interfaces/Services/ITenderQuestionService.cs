using Domain.Entities.TenderQuestions;

namespace Application.Interfaces.Services;

public interface ITenderQuestionService
{
    Task<TenderQuestion> CreateQuestionAsync(Guid tenderId, TenderQuestion question, string userId);
    Task<TenderQuestion> UpdateQuestionAsync(Guid questionId, TenderQuestion updatedQuestion, string userId);
    Task DeleteQuestionAsync(Guid questionId, string userId);
}
