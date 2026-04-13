using Domain.Entities.TenderQuestions;

namespace Application.Interfaces.Repositories;

public interface ITenderQuestionRepository
{
    Task<TenderQuestion?> GetByIdAsync(Guid id);
    Task<TenderQuestion> AddAsync(TenderQuestion question);
    Task DeleteAsync(TenderQuestion question);
    Task SaveChangesAsync();
    Task<int> GetNextOrderForTenderAsync(Guid tenderId);
}
