using System.Security.Claims;
using Application.Interfaces.Services;
using Domain.Entities.TenderQuestions;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Presentation.Models.Api;
using Presentation.Models.Questionnaire;

namespace Presentation.Controllers;

[Authorize]
[ApiController]
[Route("api/tenders/{tenderId:guid}/questionnaire")]
public class TenderQuestionApiController(ITenderQuestionService tenderQuestionService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<QuestionnaireStateViewModel>>> Get(Guid tenderId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        try
        {
            var questions = await tenderQuestionService.GetQuestionsAsync(tenderId, userId);
            return Ok(CreateSuccessResponse(CreateQuestionnaireStateViewModel(questions)));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, CreateErrorResponse(ex.Message));
        }
    }

    [HttpPost("questions")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<ApiResponse<QuestionnaireQuestionViewModel>>> Create(Guid tenderId, [FromBody] QuestionnaireQuestionInputModel model)
    {
        if (!ModelState.IsValid)
            return BadRequest(CreateValidationErrorResponse(ModelState));

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        try
        {
            var createdQuestion = await tenderQuestionService.CreateQuestionAsync(tenderId, MapToDomainQuestion(model), userId);
            return Ok(CreateSuccessResponse(MapToViewModel(createdQuestion), "De vraag is toegevoegd."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, CreateErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(CreateErrorResponse(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(CreateErrorResponse(ex.Message));
        }
    }

    [HttpPut("questions/{questionId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<ApiResponse<QuestionnaireQuestionViewModel>>> Update(Guid tenderId, Guid questionId, [FromBody] QuestionnaireQuestionInputModel model)
    {
        if (!ModelState.IsValid)
            return BadRequest(CreateValidationErrorResponse(ModelState));

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        try
        {
            var updatedQuestion = await tenderQuestionService.UpdateQuestionAsync(tenderId, questionId, MapToDomainQuestion(model), userId);
            return Ok(CreateSuccessResponse(MapToViewModel(updatedQuestion), "De vraag is bijgewerkt."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, CreateErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(CreateErrorResponse(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(CreateErrorResponse(ex.Message));
        }
    }

    [HttpDelete("questions/{questionId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid tenderId, Guid questionId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        try
        {
            await tenderQuestionService.DeleteQuestionAsync(tenderId, questionId, userId);
            return Ok(CreateSuccessResponse<object?>(null, "De vraag is verwijderd."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, CreateErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(CreateErrorResponse(ex.Message));
        }
    }

    [HttpPost("questions/reorder")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<ApiResponse<QuestionnaireStateViewModel>>> Reorder(Guid tenderId, [FromBody] QuestionnaireReorderInputModel model)
    {
        if (!ModelState.IsValid)
            return BadRequest(CreateValidationErrorResponse(ModelState));

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        try
        {
            await tenderQuestionService.ReorderQuestionsAsync(tenderId, model.OrderedQuestionIds, userId);
            var questions = await tenderQuestionService.GetQuestionsAsync(tenderId, userId);

            return Ok(CreateSuccessResponse(CreateQuestionnaireStateViewModel(questions), "De volgorde is bijgewerkt."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, CreateErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(CreateErrorResponse(ex.Message));
        }
    }

    private static TenderQuestion MapToDomainQuestion(QuestionnaireQuestionInputModel model)
    {
        return model.Type switch
        {
            QuestionType.Text => new TextQuestion
            {
                TenderId = Guid.Empty,
                Text = model.Text,
                Score = model.Score,
                Rows = model.Rows ?? 1,
                MaxLength = model.MaxLength
            },
            QuestionType.Numeric => new NumberQuestion
            {
                TenderId = Guid.Empty,
                Text = model.Text,
                Score = model.Score,
                MinValue = model.MinValue,
                MaxValue = model.MaxValue
            },
            QuestionType.Choice => new ChoiceQuestion
            {
                TenderId = Guid.Empty,
                Text = model.Text,
                Score = model.Score,
                AllowMultipleSelection = model.AllowMultipleSelection,
                Options = model.Options
                    .Select((option, index) => new TenderQuestionOption
                    {
                        Id = option.Id ?? Guid.Empty,
                        Order = index,
                        Text = option.Text
                    })
                    .ToList()
            },
            _ => throw new InvalidOperationException("Het gekozen vraagtype is ongeldig.")
        };
    }

    private static QuestionnaireQuestionViewModel MapToViewModel(TenderQuestion question)
    {
        return question switch
        {
            ChoiceQuestion choiceQuestion => new QuestionnaireQuestionViewModel
            {
                Id = choiceQuestion.Id,
                Text = choiceQuestion.Text,
                Score = choiceQuestion.Score,
                Type = choiceQuestion.Type,
                TypeLabel = choiceQuestion.AllowMultipleSelection ? "Meerkeuze" : "Enkele keuze",
                Order = choiceQuestion.Order,
                AllowMultipleSelection = choiceQuestion.AllowMultipleSelection,
                Rows = null,
                MaxLength = null,
                MinValue = null,
                MaxValue = null,
                Options = choiceQuestion.Options
                    .OrderBy(option => option.Order)
                    .Select(option => new QuestionnaireOptionViewModel
                    {
                        Id = option.Id,
                        Text = option.Text,
                        Order = option.Order
                    })
                    .ToList()
            },
            NumberQuestion numberQuestion => new QuestionnaireQuestionViewModel
            {
                Id = numberQuestion.Id,
                Text = numberQuestion.Text,
                Score = numberQuestion.Score,
                Type = numberQuestion.Type,
                TypeLabel = "Numeriek",
                Order = numberQuestion.Order,
                AllowMultipleSelection = false,
                Rows = null,
                MaxLength = null,
                MinValue = numberQuestion.MinValue,
                MaxValue = numberQuestion.MaxValue,
                Options = []
            },
            TextQuestion textQuestion => new QuestionnaireQuestionViewModel
            {
                Id = textQuestion.Id,
                Text = textQuestion.Text,
                Score = textQuestion.Score,
                Type = textQuestion.Type,
                TypeLabel = "Tekst",
                Order = textQuestion.Order,
                AllowMultipleSelection = false,
                Rows = textQuestion.Rows,
                MaxLength = textQuestion.MaxLength,
                MinValue = null,
                MaxValue = null,
                Options = []
            },
            _ => throw new InvalidOperationException("Het vraagtype wordt niet ondersteund.")
        };
    }

    private QuestionnaireStateViewModel CreateQuestionnaireStateViewModel(IEnumerable<TenderQuestion> questions)
    {
        return new QuestionnaireStateViewModel
        {
            Questions = questions.Select(MapToViewModel).ToList()
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
