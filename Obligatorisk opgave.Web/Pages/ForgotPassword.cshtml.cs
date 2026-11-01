using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Obligatorisk_opgave.Services;

namespace Obligatorisk_opgave.Pages;

public class ForgotPassword : PageModel
{
    private readonly AuthenticationService _authenticationService;

    public ForgotPassword(AuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    // Viser siden til nulstilling af password
    public void OnGet()
    {
    }

    // Kører når brugeren sender sin e-mail
    public IActionResult OnPost()
    {
        string email = Request.Form["email"].ToString();

        if (string.IsNullOrWhiteSpace(email))
        {
            TempData["ErrorMessage"] = "Please enter your email address.";

            return Page();
        }

        // Servicen opretter kun et token, hvis der findes en aktiv Customer med denne e-mail.
        // Vi viser ikke tokenet på hjemmesiden og skriver det heller ikke i logs.
        _ = _authenticationService.RequestPasswordReset(email);

        // Den samme besked vises altid.
        // Det forhindrer afsløring af, om e-mailen findes.
        TempData["SuccessMessage"] = "If an account exists with this email, a password reset link has been created.";

        return Page();
    }
}