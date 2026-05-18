using System.ComponentModel.DataAnnotations;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Presentation.Areas.Identity.Pages.Account.Manage;

[Authorize]
public class IndexModel(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ILogger<IndexModel> logger) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Vul uw huidige wachtwoord in.")]
        [DataType(DataType.Password)]
        [Display(Name = "Huidig wachtwoord")]
        public string OldPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vul uw nieuwe wachtwoord in.")]
        [StringLength(100, ErrorMessage = "Het {0} moet minimaal {2} en maximaal {1} tekens bevatten.", MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "Nieuw wachtwoord")]
        public string NewPassword { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Bevestig nieuw wachtwoord")]
        [Compare(nameof(NewPassword), ErrorMessage = "Het nieuwe wachtwoord en de bevestiging komen niet overeen.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public IActionResult OnGet() => Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return NotFound("De gebruiker kan niet worden geladen.");

        if (!await userManager.HasPasswordAsync(user))
        {
            StatusMessage = "Fout: Voor dit account kan het wachtwoord niet op deze pagina worden gewijzigd.";
            return RedirectToPage();
        }

        var changePasswordResult = await userManager.ChangePasswordAsync(user, Input.OldPassword, Input.NewPassword);
        if (!changePasswordResult.Succeeded)
        {
            foreach (var error in changePasswordResult.Errors)
            {
                ModelState.AddModelError(string.Empty, GetLocalizedErrorMessage(error));
            }

            return Page();
        }

        await signInManager.RefreshSignInAsync(user);
        logger.LogInformation("User changed their password successfully.");
        StatusMessage = "Uw wachtwoord is gewijzigd.";

        return RedirectToPage();
    }

    private static string GetLocalizedErrorMessage(IdentityError error) =>
        error.Code switch
        {
            nameof(IdentityErrorDescriber.PasswordMismatch) => "Het huidige wachtwoord is onjuist.",
            nameof(IdentityErrorDescriber.PasswordTooShort) => "Het nieuwe wachtwoord is te kort.",
            nameof(IdentityErrorDescriber.PasswordRequiresDigit) => "Het nieuwe wachtwoord moet minimaal één cijfer bevatten.",
            nameof(IdentityErrorDescriber.PasswordRequiresLower) => "Het nieuwe wachtwoord moet minimaal één kleine letter bevatten.",
            nameof(IdentityErrorDescriber.PasswordRequiresUpper) => "Het nieuwe wachtwoord moet minimaal één hoofdletter bevatten.",
            nameof(IdentityErrorDescriber.PasswordRequiresNonAlphanumeric) => "Het nieuwe wachtwoord moet minimaal één speciaal teken bevatten.",
            nameof(IdentityErrorDescriber.PasswordRequiresUniqueChars) => "Het nieuwe wachtwoord bevat te weinig verschillende tekens.",
            _ => error.Description
        };
}
