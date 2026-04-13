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
            ?? throw new KeyNotFoundException("Tender not found.");

        var (user, role) = await currentUserService.GetUserWithRoleAsync(userId);

        if (!tender.IsAccessibleBy(user, role))
            throw new UnauthorizedAccessException("User does not have access to this tender.");

        if (role != Roles.Inkoper)
            throw new UnauthorizedAccessException("Only inkopers can create questions.");

        if (!tender.CanBeEdited())
            throw new InvalidOperationException("Questions can only be changed while the tender is in Design.");

        question.TenderId = tender.Id;

        // Sets Order of options
        if (question is ChoiceQuestion choice)
            choice.SetOptions([.. choice.Options]);

        question.Validate();

        return await tenderQuestionRepository.AddAsync(question);
    }

    public async Task<TenderQuestion> UpdateQuestionAsync(Guid questionId, TenderQuestion updatedQuestion, string userId)
    {
        TenderQuestion existingQuestion = await tenderQuestionRepository.GetByIdAsync(questionId)
            ?? throw new KeyNotFoundException("Question not found.");

        Tender tender = await tenderRepository.GetByIdAsync(existingQuestion.TenderId)
            ?? throw new KeyNotFoundException("Tender not found.");

        var (user, role) = await currentUserService.GetUserWithRoleAsync(userId);

        if (!tender.IsAccessibleBy(user, role))
            throw new UnauthorizedAccessException("User does not have access to this tender.");

        if (role != Roles.Inkoper)
            throw new UnauthorizedAccessException("Only inkopers can update questions.");

        if (!tender.CanBeEdited())
            throw new InvalidOperationException("Questions can only be changed while the tender is in Design.");

        existingQuestion.Text = updatedQuestion.Text;
        existingQuestion.Score = updatedQuestion.Score;
        if (existingQuestion is ChoiceQuestion existingChoice && updatedQuestion is ChoiceQuestion updatedChoice)
        {
            existingChoice.SetOptions([.. updatedChoice.Options]);
        }
        existingQuestion.Validate();

        await tenderQuestionRepository.SaveChangesAsync();

        return existingQuestion;
    }

    public async Task DeleteQuestionAsync(Guid questionId, string userId)
    {
        TenderQuestion existingQuestion = await tenderQuestionRepository.GetByIdAsync(questionId)
            ?? throw new KeyNotFoundException("Question not found.");

        Tender tender = await tenderRepository.GetByIdAsync(existingQuestion.TenderId)
            ?? throw new KeyNotFoundException("Tender not found.");

        var (user, role) = await currentUserService.GetUserWithRoleAsync(userId);

        if (!tender.IsAccessibleBy(user, role))
            throw new UnauthorizedAccessException("User does not have access to this tender.");
        
        if (role != Roles.Inkoper)
            throw new UnauthorizedAccessException("Only inkopers can delete questions.");

        if (!tender.CanBeEdited())
            throw new InvalidOperationException("Questions can only be deleted while the tender is in Design.");

        await tenderQuestionRepository.DeleteAsync(existingQuestion);
    }

    public async Task ReorderQuestionsAsync(Guid tenderId, List<Guid> orderedQuestionIds, string userId)
    {
        Tender tender = await tenderRepository.GetByIdWithQuestionsAndOptionsAsync(tenderId)
            ?? throw new KeyNotFoundException("Tender not found.");

        var (user, role) = await currentUserService.GetUserWithRoleAsync(userId);

        if (!tender.IsAccessibleBy(user, role))
            throw new UnauthorizedAccessException("User does not have access to this tender.");

        if (role != Roles.Inkoper)
            throw new UnauthorizedAccessException("Only inkopers can reorder questions.");

        if (!tender.CanBeEdited())
            throw new InvalidOperationException("Questions can only be reorderd while the tender is in Design.");

        var questions = tender.Questions.ToList();

        var existingIds = questions.Select(q => q.Id).ToHashSet();

        if (orderedQuestionIds.Count != existingIds.Count)
            throw new InvalidOperationException("The reordered list must contain all questions exactly once.");

        var incomingIds = orderedQuestionIds.ToHashSet();

        if (!existingIds.SetEquals(incomingIds))
            throw new InvalidOperationException("The reordered list must contain all questions exactly once.");

        var orderMap = orderedQuestionIds
            .Select((id, index) => new { id, index })
            .ToDictionary(x => x.id, x => x.index);

        foreach (var question in questions)
            question.Order = orderMap[question.Id];

        await tenderQuestionRepository.SaveChangesAsync();
    }
}
