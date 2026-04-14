using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Constants;
using Domain.Entities;
using Domain.Entities.TenderQuestions;

namespace Application.Services;

public class TenderQuestionService(ITenderRepository tenderRepository, ICurrentUserService currentUserService, ITenderQuestionRepository tenderQuestionRepository) : ITenderQuestionService
{
    public async Task<List<TenderQuestion>> GetQuestionsAsync(Guid tenderId, string userId)
    {
        Tender tender = await tenderRepository.GetByIdWithQuestionsAndOptionsAsync(tenderId)
            ?? throw new KeyNotFoundException("Dit offertetraject kon niet worden gevonden.");

        var (user, role) = await currentUserService.GetUserWithRoleAsync(userId);

        if (!tender.IsAccessibleBy(user, role))
            throw new UnauthorizedAccessException("U heeft geen toegang tot dit offertetraject.");

        return tender.Questions
            .OrderBy(q => q.Order)
            .ToList();
    }

    public async Task<TenderQuestion> CreateQuestionAsync(Guid tenderId, TenderQuestion question, string userId)
    {
        var (tender, _, _) = await AuthorizeQuestionManagementAsync(tenderId, userId);

        question.TenderId = tender.Id;
        question.Order = await tenderQuestionRepository.GetNextOrderForTenderAsync(tender.Id);

        // Sets Order of options
        if (question is ChoiceQuestion choice)
            choice.SetOptions([.. choice.Options]);

        question.Validate();

        return await tenderQuestionRepository.AddAsync(question);
    }

    public async Task<TenderQuestion> UpdateQuestionAsync(Guid tenderId, Guid questionId, TenderQuestion updatedQuestion, string userId)
    {
        TenderQuestion existingQuestion = await tenderQuestionRepository.GetByIdAsync(questionId)
            ?? throw new KeyNotFoundException("Vraag niet gevonden.");

        if (existingQuestion.TenderId != tenderId)
            throw new KeyNotFoundException("Vraag niet gevonden.");

        await AuthorizeQuestionManagementAsync(tenderId, userId);

        // Update relevant fields from existing question with updated question, then validate
        existingQuestion.UpdateFrom(updatedQuestion);
        existingQuestion.Validate();

        await tenderQuestionRepository.SaveChangesAsync();

        return existingQuestion;
    }

    public async Task DeleteQuestionAsync(Guid tenderId, Guid questionId, string userId)
    {
        TenderQuestion existingQuestion = await tenderQuestionRepository.GetByIdAsync(questionId)
            ?? throw new KeyNotFoundException("Vraag niet gevonden.");

        if (existingQuestion.TenderId != tenderId)
            throw new KeyNotFoundException("Vraag niet gevonden.");

        await AuthorizeQuestionManagementAsync(tenderId, userId);

        await tenderQuestionRepository.DeleteAsync(existingQuestion);
    }

    public async Task ReorderQuestionsAsync(Guid tenderId, List<Guid> orderedQuestionIds, string userId)
    {
        var (tender, _, _) = await AuthorizeQuestionManagementAsync(tenderId, userId);

        var questions = tender.Questions.ToList();

        var existingIds = questions.Select(q => q.Id).ToHashSet();

        if (orderedQuestionIds.Count != existingIds.Count)
            throw new InvalidOperationException("De nieuwe volgorde moet alle vragen precies één keer bevatten.");

        var incomingIds = orderedQuestionIds.ToHashSet();

        if (!existingIds.SetEquals(incomingIds))
            throw new InvalidOperationException("De nieuwe volgorde moet alle vragen precies één keer bevatten.");

        var orderMap = orderedQuestionIds
            .Select((id, index) => new { id, index })
            .ToDictionary(x => x.id, x => x.index);

        foreach (var question in questions)
            question.Order = orderMap[question.Id];

        await tenderQuestionRepository.SaveChangesAsync();
    }

    private async Task<(Tender Tender, ApplicationUser User, string Role)> AuthorizeQuestionManagementAsync(Guid tenderId, string userId)
    {
        var tender = await tenderRepository.GetByIdAsync(tenderId)
            ?? throw new KeyNotFoundException("Dit offertetraject kon niet worden gevonden.");

        var (user, role) = await currentUserService.GetUserWithRoleAsync(userId);

        if (!tender.IsAccessibleBy(user, role))
            throw new UnauthorizedAccessException("U heeft geen toegang tot dit offertetraject.");

        if (role != Roles.Inkoper)
            throw new UnauthorizedAccessException("Alleen inkopers kunnen vragen beheren.");

        if (!tender.CanBeEdited())
            throw new InvalidOperationException("Vragen kunnen alleen worden aangepast zolang het offertetraject de status Ontwerp heeft.");

        return (tender, user, role);
    }
}
