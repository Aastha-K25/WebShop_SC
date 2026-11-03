using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Obligatoris.opgave.Domain.Models;
using Obligatorisk_opgave.Services;

namespace Obligatorisk_opgave.Pages;

public class ResetPassword : PageModel
{
    private readonly AuthenticationService _authenticationService;
    private readonly SessionService _sessionService;
    private readonly AuthorizationService _authorizationService;

    // Tokenet bruges i det skjulte felt på Razor-siden
    public string Token { get; private set; } = string.Empty;

    public ResetPassword(
        AuthenticationService authenticationService,
        SessionService sessionService,
        AuthorizationService authorizationService)
    {
        _authenticationService = authenticationService;
        _sessionService = sessionService;
        _authorizationService = authorizationService;
    }

    // Henter tokenet fra reset-linket
    public IActionResult OnGet(string? token)
    {
        User? user = _sessionService.GetCurrentUser();

        // Admin må ikke bruge Customer password reset.
        if (!_authorizationService.CanUseCustomerPasswordReset(user))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            TempData["ErrorMessage"] =
                "The password reset link is invalid or has expired.";

            return RedirectToPage("/ForgotPassword");
        }

        Token = token;

        return Page();
    }

    // Kører når kunden sender det nye password
    public IActionResult OnPost()
    {
        User? user = _sessionService.GetCurrentUser();

        // Admin må ikke bruge Customer password reset.
        if (!_authorizationService.CanUseCustomerPasswordReset(user))
        {
            return Forbid();
        }

        Token = Request.Form["token"].ToString();

        string newPassword = Request.Form["newPassword"].ToString();

        string confirmPassword = Request.Form["confirmPassword"].ToString();

        if (string.IsNullOrWhiteSpace(Token) ||
            string.IsNullOrWhiteSpace(newPassword) ||
            string.IsNullOrWhiteSpace(confirmPassword))
        {
            TempData["ErrorMessage"] =
                "Please fill out all fields.";

            return Page();
        }

        // De to passwords skal være ens
        if (newPassword != confirmPassword)
        {
            TempData["ErrorMessage"] =
                "The passwords do not match.";

            return Page();
        }

        string? errorMessage =
            _authenticationService.ResetPassword(
                Token,
                newPassword);

        if (errorMessage != null)
        {
            TempData["ErrorMessage"] =
                errorMessage;

            return Page();
        }

        TempData["SuccessMessage"] =
            "Your password has been reset. You can now log in.";

        return RedirectToPage("/Login");
    }
}
