using Application.Interfaces.Services;
using Application.Models.UserManagement;
using Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Models.UserManagement;

namespace Presentation.Controllers;

[Authorize(Roles = Roles.Beheerder)]
public class UserManagementController(IUserManagementService userManagementService) : AuthenticatedControllerBase
{
    public async Task<IActionResult> Index(string? search)
    {
        var users = await userManagementService.GetUsersAsync(new UserManagementQuery
        {
            Search = search
        });

        return View(new UserManagementIndexViewModel
        {
            Search = search?.Trim() ?? string.Empty,
            Users = users
        });
    }
}
