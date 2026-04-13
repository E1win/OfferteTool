using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Constants;
using Domain.Entities;
using Domain.Entities.TenderQuestions;

namespace Application.Services;

public class TenderQuestionService(ITenderRepository tenderRepository, ICurrentUserService currentUserService, ITenderQuestionRepository tenderQuestionRepository) : ITenderQuestionService
{
    public async Task<TenderQuestion> CreateQuestionAsync(Guid tenderId, TenderQuestion question, string userId)
    {
        Tender tender = await tenderRepository.GetByIdAsync(tenderId)
            ?? throw new KeyNotFoundException("Dit offertetraject kon niet worden gevonden.");

        var (user, role) = await currentUserService.GetUserWithRoleAsync(userId);

        if (!tender.IsAccessibleBy(user, role))
            throw new UnauthorizedAccessException("U heeft geen toegang tot dit offertetraject.");

        if (role != Roles.Inkoper)
            throw new UnauthorizedAccessException("Alleen inkopers kunnen vragen aanmaken.");

        if (!tender.CanBeEdited())
            throw new InvalidOperationException("Vragen kunnen alleen worden aangepast zolang het offertetraject de status Ontwerp heeft.");

        question.TenderId = tender.Id;
        question.Order = await tenderQuestionRepository.GetNextOrderForTenderAsync(tender.Id);

        // Sets Order of options
        if (question is ChoiceQuestion choice)
            choice.SetOptions([.. choice.Options]);

        question.Validate();

        return await tenderQuestionRepository.AddAsync(question);
    }

    public async Task<TenderQuestion> UpdateQuestionAsync(Guid questionId, TenderQuestion updatedQuestion, string userId)
    {
        TenderQuestion existingQuestion = await tenderQuestionRepository.GetByIdAsync(questionId)
            ?? throw new KeyNotFoundException("Vraag niet gevonden.");

        Tender tender = await tenderRepository.GetByIdAsync(existingQuestion.TenderId)
            ?? throw new KeyNotFoundException("Dit offertetraject kon niet worden gevonden.");

        var (user, role) = await currentUserService.GetUserWithRoleAsync(userId);

        if (!tender.IsAccessibleBy(user, role))
            throw new UnauthorizedAccessException("U heeft geen toegang tot dit offertetraject.");

        if (role != Roles.Inkoper)
            throw new UnauthorizedAccessException("Alleen inkopers kunnen vragen wijzigen.");

        if (!tender.CanBeEdited())
            throw new InvalidOperationException("Vragen kunnen alleen worden aangepast zolang het offertetraject de status Ontwerp heeft.");

        existingQuestion.Text = updatedQuestion.Text;
        existingQuestion.Score = updatedQuestion.Score;
        if (existingQuestion is ChoiceQuestion existingChoice && updatedQuestion is ChoiceQuestion updatedChoice)
        {
            existingChoice.SetOptions([.. updatedChoice.Options]);
        }

        CopyTypeSpecificProperties(existingQuestion, updatedQuestion);
        existingQuestion.Validate();

        await tenderQuestionRepository.SaveChangesAsync();

        return existingQuestion;
    }

    private static void CopyTypeSpecificProperties(TenderQuestion existingQuestion, TenderQuestion updatedQuestion)
    {
        Type existingType = existingQuestion.GetType();
        Type updatedType = updatedQuestion.GetType();

        if (existingType != updatedType)
            return;

        var properties = existingType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly);

        foreach (var property in properties)
        {
            if (!property.CanRead || !property.CanWrite)
                continue;
            if (property.GetIndexParameters().Length > 0)
                continue;
            if (property.Name == nameof(ChoiceQuestion.Options))
                continue;
            property.SetValue(existingQuestion, property.GetValue(updatedQuestion));
        }
    }

    public async Task DeleteQuestionAsync(Guid questionId, string userId)
    {
        TenderQuestion existingQuestion = await tenderQuestionRepository.GetByIdAsync(questionId)
            ?? throw new KeyNotFoundException("Vraag niet gevonden.");

        Tender tender = await tenderRepository.GetByIdAsync(existingQuestion.TenderId)
            ?? throw new KeyNotFoundException("Dit offertetraject kon niet worden gevonden.");

        var (user, role) = await currentUserService.GetUserWithRoleAsync(userId);

        if (!tender.IsAccessibleBy(user, role))
            throw new UnauthorizedAccessException("U heeft geen toegang tot dit offertetraject.");
        
        if (role != Roles.Inkoper)
            throw new UnauthorizedAccessException("Alleen inkopers kunnen vragen verwijderen.");

        if (!tender.CanBeEdited())
            throw new InvalidOperationException("Vragen kunnen alleen worden verwijderd zolang het offertetraject de status Ontwerp heeft.");

        await tenderQuestionRepository.DeleteAsync(existingQuestion);
    }

    public async Task ReorderQuestionsAsync(Guid tenderId, List<Guid> orderedQuestionIds, string userId)
    {
        Tender tender = await tenderRepository.GetByIdWithQuestionsAndOptionsAsync(tenderId)
            ?? throw new KeyNotFoundException("Dit offertetraject kon niet worden gevonden.");

        var (user, role) = await currentUserService.GetUserWithRoleAsync(userId);

        if (!tender.IsAccessibleBy(user, role))
            throw new UnauthorizedAccessException("U heeft geen toegang tot dit offertetraject.");

        if (role != Roles.Inkoper)
            throw new UnauthorizedAccessException("Alleen inkopers kunnen vragen herordenen.");

        if (!tender.CanBeEdited())
            throw new InvalidOperationException("Vragen kunnen alleen worden herordend zolang het offertetraject de status Ontwerp heeft.");

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
}
