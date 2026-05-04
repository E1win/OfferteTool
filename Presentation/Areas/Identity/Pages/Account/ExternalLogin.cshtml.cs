using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Presentation.Areas.Identity.Pages.Account;

public class ExternalLoginModel : PageModel
{
    public IActionResult OnGet() => NotFound();

    public IActionResult OnPost() => NotFound();

    public IActionResult OnGetCallback() => NotFound();

    public IActionResult OnPostConfirmation() => NotFound();
}
