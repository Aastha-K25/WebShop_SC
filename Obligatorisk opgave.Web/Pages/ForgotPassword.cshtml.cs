using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Obligatoris.opgave.Domain.Models;
using Obligatorisk_opgave.Services;

namespace Obligatorisk_opgave.Pages;

public class ForgotPassword : PageModel
{
    private readonly AuthenticationService _authenticationService;
    private readonly SessionService _sessionService;
    private readonly AuthorizationService _authorizationService;

    public ForgotPassword(
        AuthenticationService authenticationService,
        SessionService sessionService,
        AuthorizationService authorizationService)
    {
        _authenticationService = authenticationService;
        _sessionService = sessionService;
        _authorizationService = authorizationService;
    }

    // Viser siden til nulstilling af password.
    public IActionResult OnGet()
    {
        User? user = _sessionService.GetCurrentUser();

        // Admin må ikke bruge Customer password reset.
        if (!_authorizationService.CanUseCustomerPasswordReset(user))
        {
            return Forbid();
        }

        return Page();
    }

    // Kører, når brugeren indsender sin e-mail.
    public IActionResult OnPost()
    {
        User? user = _sessionService.GetCurrentUser();

        // Admin må ikke bruge Customer password reset.
        if (!_authorizationService.CanUseCustomerPasswordReset(user))
        {
            return Forbid();
        }

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
            "Password reset by email is not available right now.";

        return Page();
    }
}
