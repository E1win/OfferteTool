using Domain.Entities.TenderQuestions;

namespace Application.Interfaces.Services;

public interface ITenderQuestionService
{
    Task<TenderQuestion> CreateQuestionAsync(Guid tenderId, TenderQuestion question, string userId);
}
