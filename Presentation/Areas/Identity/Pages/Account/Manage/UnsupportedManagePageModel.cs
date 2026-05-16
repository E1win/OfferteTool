using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Presentation.Areas.Identity.Pages.Account.Manage;

[Authorize]
public class UnsupportedManagePageModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage("./Index");

    public IActionResult OnPost() => RedirectToPage("./Index");
}
