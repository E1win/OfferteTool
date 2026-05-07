using Application.Interfaces.Services;
using Application.Models.SecurityAudit;
using System.ComponentModel.DataAnnotations;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Presentation.Areas.Identity.Pages.Account;

public class LoginModel(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    ISecurityAuditService securityAuditService,
    ILogger<LoginModel> logger) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string ReturnUrl { get; set; } = string.Empty;

    public class InputModel
    {
        [Required(ErrorMessage = "Vul uw e-mailadres in.")]
        [EmailAddress(ErrorMessage = "Vul een geldig e-mailadres in.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vul uw wachtwoord in.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Onthoud mij")]
        public bool RememberMe { get; set; }
    }

    public async Task OnGetAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? Url.Content("~/");
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? Url.Content("~/");

        if (!ModelState.IsValid)
            return Page();

        var result = await signInManager.PasswordSignInAsync(
            Input.Email,
            Input.Password,
            Input.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            var user = await userManager.FindByEmailAsync(Input.Email);

            if (user is null || !user.IsActive)
            {
                await signInManager.SignOutAsync();
                await LogLoginAsync(SecurityAuditEventType.LoginRejectedInactiveUser, SecurityAuditOutcome.Denied, user);
                ModelState.AddModelError(string.Empty, "Ongeldige inlogpoging.");
                return Page();
            }

            await LogLoginAsync(SecurityAuditEventType.LoginSucceeded, SecurityAuditOutcome.Success, user);
            logger.LogInformation("User logged in.");
            return LocalRedirect(ReturnUrl);
        }

        if (result.IsLockedOut)
        {
            await LogLoginAsync(SecurityAuditEventType.LoginLockedOut, SecurityAuditOutcome.Denied);
            logger.LogWarning("User account locked out.");
            return RedirectToPage("./Lockout");
        }

        await LogLoginAsync(SecurityAuditEventType.LoginFailed, SecurityAuditOutcome.Failure);
        ModelState.AddModelError(string.Empty, "Ongeldige inlogpoging.");
        return Page();
    }

    private async Task LogLoginAsync(
        SecurityAuditEventType eventType,
        SecurityAuditOutcome outcome,
        ApplicationUser? user = null)
    {
        await securityAuditService.LogAsync(new SecurityAuditEvent
        {
            EventType = eventType,
            Outcome = outcome,
            ActorUserId = user?.Id,
            ActorIdentifier = NormalizeIdentifier(Input.Email),
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString(),
            TraceId = HttpContext.TraceIdentifier
        });
    }

    private static string? NormalizeIdentifier(string? identifier) =>
        string.IsNullOrWhiteSpace(identifier)
            ? null
            : identifier.Trim().ToLowerInvariant();
}
