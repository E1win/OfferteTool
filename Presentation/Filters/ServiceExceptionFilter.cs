using Application.Interfaces.Services;
using Application.Models.SecurityAudit;
using Domain.Enums;
using Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Presentation.Models.Api;
using System.Security.Claims;

namespace Presentation.Filters;

/// <summary>
/// Handles infrastructural exceptions (NotFound, Unauthorized).
/// Business validation exceptions should be handled by controllers for
/// context-specific MVC error messages, but can be mapped generically for API endpoints.
/// </summary>
public class ServiceExceptionFilter(
    ISecurityAuditService securityAuditService) : IAsyncExceptionFilter
{
    public async Task OnExceptionAsync(ExceptionContext context)
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

        if (context.Exception is UnauthorizedAccessException)
            await LogAccessDeniedAsync(context);

        if (context.Result != null)
            context.ExceptionHandled = true;
    }

    private async Task LogAccessDeniedAsync(ExceptionContext context)
    {
        var httpContext = context.HttpContext;

        await securityAuditService.TryLogAsync(new SecurityAuditEvent
        {
            EventType = SecurityAuditEventType.AccessDenied,
            Outcome = SecurityAuditOutcome.Denied,
            ActorUserId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
            IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = httpContext.Request.Headers.UserAgent.ToString(),
            TraceId = httpContext.TraceIdentifier,
            Details = new Dictionary<string, string>
            {
                ["method"] = httpContext.Request.Method,
                ["path"] = httpContext.Request.Path.Value ?? string.Empty
            }
        });
    }
}
