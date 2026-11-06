
using Microsoft.AspNetCore.Mvc; // Bruges til at håndtere sider og handlinger
using Microsoft.AspNetCore.Mvc.RazorPages; // Bruges til Razor Pages
using Obligatoris.opgave.Domain.Models; // Giver adgang til vores modeller
using Obligatorisk_opgave.Services; // Giver adgang til vores services

namespace Obligatorisk_opgave.Pages;

// PageModel som håndterer glemt password
public class ForgotPassword : PageModel
{
    // Bruges til at anmode om nulstilling af password
    private readonly AuthenticationService _authenticationService;

    // Bruges til at finde den bruger som er logget ind
    private readonly SessionService _sessionService;

    // Bruges til at kontrollere om brugeren har adgang
    private readonly AuthorizationService _authorizationService;

    // Constructor som modtager de services vi skal bruge
    public ForgotPassword(
        AuthenticationService authenticationService,
        SessionService sessionService,
        AuthorizationService authorizationService)
    {
        // Gemmer vores authentication service
        _authenticationService = authenticationService;

        // Gemmer vores session service
        _sessionService = sessionService;

        // Gemmer vores authorization service
        _authorizationService = authorizationService;
    }

    // OnGet kører når brugeren åbner siden til glemt password
    public IActionResult OnGet()
    {
        // Henter brugeren som er logget ind
        User? user = _sessionService.GetCurrentUser();

        // Kontrollerer om brugeren må bruge password reset
        // Admin må ikke bruge denne funktion til kunder
        if (!_authorizationService.CanUseCustomerPasswordReset(user))
        {
            // Afviser adgang hvis brugeren ikke har tilladelse
            return Forbid();
        }

        // Viser siden hvor brugeren kan indtaste sin email
        return Page();
    }

    // OnPost kører når brugeren sender sin email
    public IActionResult OnPost()
    {
        // Henter brugeren som er logget ind
        User? user = _sessionService.GetCurrentUser();

        // Kontrollerer igen om brugeren må bruge password reset
        if (!_authorizationService.CanUseCustomerPasswordReset(user))
        {
            // Afviser adgang hvis brugeren ikke har tilladelse
            return Forbid();
        }

        // Henter email fra formularen
        string email = Request.Form["email"].ToString();

        // Kontrollerer om email feltet er tomt
        if (string.IsNullOrWhiteSpace(email))
        {
            // Gemmer en fejlbesked hvis brugeren ikke har skrevet en email
            TempData["ErrorMessage"] = "Please enter your email address.";

            // Viser siden igen så brugeren kan indtaste sin email
            return Page();
        }

        // Sender email til AuthenticationService
        // Servicen håndterer anmodningen om password reset
        // Vi bruger ikke returværdien her
        _ = _authenticationService.RequestPasswordReset(email);

        // Viser den samme besked uanset om email findes i databasen
        // Det gør det sværere at finde ud af hvilke kunder der har en konto
        // Password reset via email er endnu ikke tilgængeligt
        TempData["SuccessMessage"] = "Password reset by email is not available right now.";

        // Viser siden igen med beskeden
        return Page();
    }
}
