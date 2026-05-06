using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Models.Questionnaire;
using Domain.Entities;
using Domain.Entities.TenderQuestions;
using Domain.Enums;
using Domain.Exceptions;

namespace Application.Services;

public class TenderQuestionService(
    ITenderRepository tenderRepository,
    ICurrentUserService currentUserService,
    ITenderQuestionRepository tenderQuestionRepository,
    ITenderChangeLogRepository tenderChangeLogRepository) : ITenderQuestionService
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

    public async Task<TenderQuestion> AmendQuestionTextAsync(Guid tenderId, Guid questionId, QuestionTextAmendment amendment, string userId)
    {
        TenderQuestion existingQuestion = await tenderQuestionRepository.GetByIdAsync(questionId)
            ?? throw new KeyNotFoundException("Vraag niet gevonden.");

        if (existingQuestion.TenderId != tenderId)
            throw new KeyNotFoundException("Vraag niet gevonden.");

        var (tender, user, role) = await AuthorizePublishedQuestionTextAmendmentAsync(tenderId, userId);
        var newText = amendment.Text.Trim();

        if (string.IsNullOrWhiteSpace(newText))
            throw new BusinessRuleViolationException("Vul een vraag in.");

        if (newText.Length > 512)
            throw new BusinessRuleViolationException("Een vraag mag maximaal 512 tekens bevatten.");

        if (string.Equals(existingQuestion.Text, newText, StringComparison.Ordinal))
            return existingQuestion;

        var oldText = existingQuestion.Text;
        existingQuestion.Text = newText;

        await tenderChangeLogRepository.AddAsync(new TenderChangeLog
        {
            TenderId = tender.Id,
            Type = TenderChangeLogType.QuestionTextAmended,
            FieldName = "Question.Text",
            OldValue = oldText,
            NewValue = newText,
            SupplierVisibleMessage = $"Vraag {existingQuestion.Order + 1} is gewijzigd van \"{oldText}\" naar \"{newText}\".",
            ChangedAtUtc = DateTimeOffset.UtcNow,
            ChangedByUserId = user.Id,
            ChangedByDisplayName = GetDisplayName(user)
        });

        await tenderChangeLogRepository.SaveChangesAsync();

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
        var (tender, _, _) = await AuthorizeQuestionManagementAsync(tenderId, userId, includeQuestions: true);

        var questions = tender.Questions.ToList();

        var existingIds = questions.Select(q => q.Id).ToHashSet();

        if (orderedQuestionIds.Count != existingIds.Count)
            throw new BusinessRuleViolationException("De nieuwe volgorde moet alle vragen precies één keer bevatten.");

        var incomingIds = orderedQuestionIds.ToHashSet();

        if (!existingIds.SetEquals(incomingIds))
            throw new BusinessRuleViolationException("De nieuwe volgorde moet alle vragen precies één keer bevatten.");

        var orderMap = orderedQuestionIds
            .Select((id, index) => new { id, index })
            .ToDictionary(x => x.id, x => x.index);

        var temporaryOrderOffset = questions
            .Select(question => question.Order)
            .DefaultIfEmpty(-1)
            .Max() + questions.Count + 1;

        // Apply temporary unique orders first to avoid unique-index collisions while swapping positions.
        foreach (var question in questions)
            question.Order = temporaryOrderOffset + orderMap[question.Id];

        await tenderQuestionRepository.SaveChangesAsync();

        foreach (var question in questions)
            question.Order = orderMap[question.Id];

        await tenderQuestionRepository.SaveChangesAsync();
    }

    private async Task<(Tender Tender, ApplicationUser User, string Role)> AuthorizeQuestionManagementAsync(
        Guid tenderId,
        string userId,
        bool includeQuestions = false)
    {
        Tender? tender = includeQuestions
            ? await tenderRepository.GetByIdWithQuestionsAndOptionsAsync(tenderId)
            : await tenderRepository.GetByIdAsync(tenderId);

        if (tender is null)
            throw new KeyNotFoundException("Dit offertetraject kon niet worden gevonden.");

        var (user, role) = await currentUserService.GetUserWithRoleAsync(userId);

        if (!tender.CanBeManagedBy(user, role))
            throw new UnauthorizedAccessException("U kunt dit offertetraject niet beheren.");

        if (!tender.CanBeEdited())
            throw new BusinessRuleViolationException("Vragen kunnen alleen worden aangepast zolang het offertetraject de status Ontwerp heeft.");

        return (tender, user, role);
    }

    private async Task<(Tender Tender, ApplicationUser User, string Role)> AuthorizePublishedQuestionTextAmendmentAsync(
        Guid tenderId,
        string userId)
    {
        Tender? tender = await tenderRepository.GetByIdAsync(tenderId);

        if (tender is null)
            throw new KeyNotFoundException("Dit offertetraject kon niet worden gevonden.");

        var (user, role) = await currentUserService.GetUserWithRoleAsync(userId);

        if (!tender.CanBeManagedBy(user, role))
            throw new UnauthorizedAccessException("U kunt dit offertetraject niet beheren.");

        if (!tender.CanBeAmended())
            throw new BusinessRuleViolationException("Vraagteksten kunnen alleen worden aangepast zolang het offertetraject open staat.");

        return (tender, user, role);
    }

    private static string GetDisplayName(ApplicationUser user)
    {
        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        if (!string.IsNullOrWhiteSpace(fullName))
            return fullName;

        if (!string.IsNullOrWhiteSpace(user.Email))
            return user.Email!;

        return user.UserName ?? "Onbekende gebruiker";
    }
}
