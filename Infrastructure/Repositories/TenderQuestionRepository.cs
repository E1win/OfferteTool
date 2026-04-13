using Application.Interfaces.Repositories;
using Domain.Entities.TenderQuestions;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class TenderQuestionRepository(AppDbContext dbContext) : ITenderQuestionRepository
{
    public async Task<TenderQuestion?> GetByIdAsync(Guid id) =>
        await dbContext.Set<TenderQuestion>()
            .Include(q => ((ChoiceQuestion)q).Options)
            .FirstOrDefaultAsync(q => q.Id == id);

    public async Task<TenderQuestion> AddAsync(TenderQuestion question)
    {
        dbContext.Set<TenderQuestion>().Add(question);
        await dbContext.SaveChangesAsync();
        return question;
    }

    public async Task DeleteAsync(TenderQuestion question)
    {
        dbContext.Set<TenderQuestion>().Remove(question);
        await dbContext.SaveChangesAsync();
    }

    public async Task SaveChangesAsync()
    {
        await dbContext.SaveChangesAsync();
    }
}
