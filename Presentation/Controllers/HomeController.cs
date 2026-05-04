using Microsoft.AspNetCore.Mvc;
using Presentation.Models.Shared;
using System.Diagnostics;
using Domain.Constants;

namespace Presentation.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole(Roles.Beheerder))
                    return RedirectToAction("Index", "UserManagement");

                return RedirectToAction("Index", "Tender");
            }

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
