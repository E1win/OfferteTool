using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Presentation.Controllers;

[Authorize]
public abstract class AuthenticatedControllerBase : Controller
{
    protected string UserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Er is geen aangemelde gebruiker beschikbaar voor deze aanvraag.");
}
