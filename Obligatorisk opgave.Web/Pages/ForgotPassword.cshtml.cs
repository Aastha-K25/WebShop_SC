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

    // Viser siden til nulstilling af password.
    public void OnGet()
    {
    }

    // Kører, når brugeren indsender sin e-mail.
    public IActionResult OnPost()
    {
        string email = Request.Form["email"].ToString();

        // Kontrollerer, at brugeren har skrevet en e-mailadresse.
        if (string.IsNullOrWhiteSpace(email))
        {
            TempData["ErrorMessage"] =
                "Please enter your email address.";

            return Page();
        }

        // Opretter kun et token, hvis der findes en aktiv Customer.
        // Tokenet vises ikke på hjemmesiden og skrives ikke i logs.
        _ = _authenticationService.RequestPasswordReset(email);

        // Samme besked vises, uanset om e-mailadressen findes.
        // Det forhindrer andre i at undersøge, hvilke kunder der findes.
        TempData["SuccessMessage"] =
            "If an account exists with this email, " +
            "a password reset link has been created.";

        return Page();
    }
}