using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Presentation.Areas.Identity.Pages.Account;

public class ForgotPasswordConfirmationModel : PageModel
{
    public IActionResult OnGet() => NotFound();

    public IActionResult OnPost() => NotFound();
}
