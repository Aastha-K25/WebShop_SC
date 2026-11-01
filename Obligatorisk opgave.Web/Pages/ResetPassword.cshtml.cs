using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Obligatorisk_opgave.Services;

namespace Obligatorisk_opgave.Pages;

public class ResetPassword : PageModel
{
    private readonly AuthenticationService _authenticationService;

    // Tokenet bruges i det skjulte felt på Razor-siden
    public string Token { get; private set; } = string.Empty;

    public ResetPassword(AuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    // Henter tokenet fra reset-linket
    public IActionResult OnGet(string? token)
    {
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

        string? errorMessage = _authenticationService.ResetPassword(Token, newPassword);

        if (errorMessage != null)
        {
            TempData["ErrorMessage"] = errorMessage; 
            return Page();
        }

        TempData["SuccessMessage"] =
            "Your password has been reset. You can now log in.";

        return RedirectToPage("/Login");
    }
}