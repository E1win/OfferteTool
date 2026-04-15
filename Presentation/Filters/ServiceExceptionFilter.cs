using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Presentation.Models.Api;

namespace Presentation.Filters;

/// <summary>
/// Handles infrastructural exceptions (NotFound, Unauthorized).
/// Business validation exceptions (InvalidOperationException, ArgumentException) 
/// should be handled by controllers for context-specific error messages.
/// </summary>
public class ServiceExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var isApiController = context.HttpContext.Request.Path.StartsWithSegments("/api");

        context.Result = context.Exception switch
        {
            // NotFound: Resource doesn't exist (generic handling)
            KeyNotFoundException ex when isApiController =>
                new NotFoundObjectResult(new ApiResponse<object?>
                {
                    Message = ex.Message,
                    Errors = []
                }),

            KeyNotFoundException =>
                new NotFoundResult(),

            // Unauthorized: Access denied (generic handling)
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

            // Business validation for API: can be handled generically
            InvalidOperationException ex when isApiController =>
                new BadRequestObjectResult(new ApiResponse<object?>
                {
                    Message = ex.Message,
                    Errors = []
                }),

            ArgumentException ex when isApiController =>
                new BadRequestObjectResult(new ApiResponse<object?>
                {
                    Message = ex.Message,
                    Errors = []
                }),

            // For MVC controllers: business validations need context-specific handling
            // Let controller handle to show proper view with error message
            _ => null
        };

        if (context.Result != null)
            context.ExceptionHandled = true;
    }
}