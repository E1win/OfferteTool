using Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Presentation.Models.Api;

namespace Presentation.Filters;

/// <summary>
/// Handles infrastructural exceptions (NotFound, Unauthorized).
/// Business validation exceptions should be handled by controllers for
/// context-specific MVC error messages, but can be mapped generically for API endpoints.
/// </summary>
public class ServiceExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var isApiController = context.HttpContext.Request.Path.StartsWithSegments("/api");

        context.Result = context.Exception switch
        {
            KeyNotFoundException ex when isApiController =>
                new NotFoundObjectResult(new ApiResponse<object?>
                {
                    Message = ex.Message,
                    Errors = []
                }),

            KeyNotFoundException =>
                new NotFoundResult(),

            UnauthorizedAccessException ex when isApiController =>
                new ObjectResult(new ApiResponse<object?>
                {
                    Message = ex.Message,
                    Errors = []
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                },

            UnauthorizedAccessException =>
                new ForbidResult(),

            BusinessRuleViolationException ex when isApiController =>
                new BadRequestObjectResult(new ApiResponse<object?>
                {
                    Message = ex.Message,
                    Errors = []
                }),

            _ => null
        };

        if (context.Result != null)
            context.ExceptionHandled = true;
    }
}
