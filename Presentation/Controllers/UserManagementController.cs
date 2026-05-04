using Application.Interfaces.Services;
using Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Models.UserManagement;

namespace Presentation.Controllers;

[Authorize(Roles = Roles.Beheerder)]
public class UserManagementController(IUserManagementService userManagementService) : AuthenticatedControllerBase
{
    public async Task<IActionResult> Index()
    {
        var users = await userManagementService.GetUsersAsync();

        return View(new UserManagementIndexViewModel
        {
            Users = users
        });
    }
}
