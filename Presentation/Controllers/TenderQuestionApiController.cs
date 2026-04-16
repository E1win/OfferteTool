using Application.Interfaces.Services;
using Domain.Entities.TenderQuestions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Presentation.Mappings;
using Presentation.Models.Api;
using Presentation.Models.Questionnaire;

namespace Presentation.Controllers;

[ApiController]
[Route("api/tenders/{tenderId:guid}/questionnaire")]
public class TenderQuestionApiController(
    ITenderQuestionService tenderQuestionService) : AuthenticatedApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<QuestionnaireStateViewModel>>> Get(Guid tenderId)
    {
        var questions = await tenderQuestionService.GetQuestionsAsync(tenderId, UserId);
        return Ok(CreateSuccessResponse(CreateQuestionnaireStateViewModel(questions)));
    }

    [HttpPost("questions")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<ApiResponse<QuestionnaireQuestionViewModel>>> Create(Guid tenderId, [FromBody] QuestionnaireQuestionInputModel model)
    {
        if (!ModelState.IsValid)
            return BadRequest(CreateValidationErrorResponse(ModelState));

        var createdQuestion = await tenderQuestionService.CreateQuestionAsync(tenderId, TenderQuestionMapper.ToEntity(model), UserId);

        return Ok(CreateSuccessResponse(TenderQuestionMapper.ToViewModel(createdQuestion), "De vraag is toegevoegd."));
    }

    [HttpPut("questions/{questionId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<ApiResponse<QuestionnaireQuestionViewModel>>> Update(Guid tenderId, Guid questionId, [FromBody] QuestionnaireQuestionInputModel model)
    {
        if (!ModelState.IsValid)
            return BadRequest(CreateValidationErrorResponse(ModelState));

        var updatedQuestion = await tenderQuestionService.UpdateQuestionAsync(tenderId, questionId, TenderQuestionMapper.ToEntity(model), UserId);
        return Ok(CreateSuccessResponse(TenderQuestionMapper.ToViewModel(updatedQuestion), "De vraag is bijgewerkt."));
    }

    [HttpDelete("questions/{questionId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid tenderId, Guid questionId)
    {
        await tenderQuestionService.DeleteQuestionAsync(tenderId, questionId, UserId);
        return Ok(CreateSuccessResponse<object?>(null, "De vraag is verwijderd."));
    }

    [HttpPost("questions/reorder")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<ApiResponse<QuestionnaireStateViewModel>>> Reorder(Guid tenderId, [FromBody] QuestionnaireReorderInputModel model)
    {
        if (!ModelState.IsValid)
            return BadRequest(CreateValidationErrorResponse(ModelState));

        await tenderQuestionService.ReorderQuestionsAsync(tenderId, model.OrderedQuestionIds, UserId);
        var questions = await tenderQuestionService.GetQuestionsAsync(tenderId, UserId);

        return Ok(CreateSuccessResponse(CreateQuestionnaireStateViewModel(questions), "De volgorde is bijgewerkt."));
    }

    private QuestionnaireStateViewModel CreateQuestionnaireStateViewModel(IEnumerable<TenderQuestion> questions)
    {
        return new QuestionnaireStateViewModel
        {
            Questions = questions.Select(TenderQuestionMapper.ToViewModel).ToList()
        };
    }

    private static ApiResponse<T> CreateSuccessResponse<T>(T? data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Data = data,
            Message = message,
            Errors = []
        };
    }

    private static ApiResponse<object?> CreateErrorResponse(string message, IEnumerable<string>? errors = null)
    {
        return new ApiResponse<object?>
        {
            Data = null,
            Message = message,
            Errors = errors?.ToList() ?? []
        };
    }

    private static ApiResponse<object?> CreateValidationErrorResponse(ModelStateDictionary modelState)
    {
        var errors = modelState.Values
            .SelectMany(entry => entry.Errors)
            .Select(error => error.ErrorMessage)
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .Distinct()
            .ToList();

        return CreateErrorResponse(
            errors.Count switch
            {
                0 => "Controleer de ingevulde gegevens en probeer het opnieuw.",
                1 => errors[0],
                _ => "Controleer de ingevulde gegevens en pas de gemarkeerde onderdelen aan."
            },
            errors);
    }
}
