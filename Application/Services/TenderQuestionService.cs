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
}
