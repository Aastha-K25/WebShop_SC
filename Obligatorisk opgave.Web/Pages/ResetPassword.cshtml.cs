
using Microsoft.AspNetCore.Mvc; // Bruges til at håndtere sider og handlinger
using Microsoft.AspNetCore.Mvc.RazorPages; // Bruges til Razor Pages
using Obligatoris.opgave.Domain.Models; // Giver adgang til vores modeller
using Obligatorisk_opgave.Services; // Giver adgang til vores services

namespace Obligatorisk_opgave.Pages;

// PageModel som håndterer nulstilling af password
public class ResetPassword : PageModel
{
    // Bruges til at kontrollere og nulstille password
    private readonly AuthenticationService _authenticationService;

    // Bruges til at finde den bruger som er logget ind
    private readonly SessionService _sessionService;

    // Bruges til at kontrollere om brugeren har adgang
    private readonly AuthorizationService _authorizationService;

    // Gemmer tokenet som bruges til at nulstille password
    // private set betyder at det kun kan ændres inde i klassen
    public string Token { get; private set; } = string.Empty;

    // Constructor som modtager de services vi skal bruge
    public ResetPassword(
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

    // OnGet kører når brugeren åbner linket til password reset
    public IActionResult OnGet(string? token)
    {
        // Henter brugeren som er logget ind
        User? user = _sessionService.GetCurrentUser();

        // Kontrollerer om brugeren må nulstille password
        // Admin må ikke bruge denne funktion til kunder
        if (!_authorizationService.CanUseCustomerPasswordReset(user))
        {
            // Afviser adgang hvis brugeren ikke har tilladelse
            return Forbid();
        }

        // Kontrollerer om der mangler et token i linket
        if (string.IsNullOrWhiteSpace(token))
        {
            // Gemmer en fejlbesked hvis tokenet mangler
            TempData["ErrorMessage"] = "The password reset link is invalid or has expired.";

            // Sender brugeren tilbage til siden med glemt password
            return RedirectToPage("/ForgotPassword");
        }

        // Gemmer tokenet så det kan bruges i formularen
        Token = token;

        // Viser siden hvor brugeren kan indtaste et nyt password
        return Page();
    }

    // OnPost kører når brugeren sender det nye password
    public IActionResult OnPost()
    {
        // Henter brugeren som er logget ind
        User? user = _sessionService.GetCurrentUser();

        // Kontrollerer igen om brugeren må nulstille password
        if (!_authorizationService.CanUseCustomerPasswordReset(user))
        {
            // Afviser adgang hvis brugeren ikke har tilladelse
            return Forbid();
        }

        // Henter tokenet fra formularens skjulte felt
        Token = Request.Form["token"].ToString();

        // Henter det nye password fra formularen
        string newPassword = Request.Form["newPassword"].ToString();

        // Henter bekræftelsen af det nye password
        string confirmPassword = Request.Form["confirmPassword"].ToString();

        // Kontrollerer om et af felterne er tomme
        if (string.IsNullOrWhiteSpace(Token) ||
            string.IsNullOrWhiteSpace(newPassword) ||
            string.IsNullOrWhiteSpace(confirmPassword))
        {
            // Gemmer en fejlbesked hvis et felt mangler
            TempData["ErrorMessage"] = "Please fill out all fields.";

            // Viser siden igen så brugeren kan udfylde felterne
            return Page();
        }

        // Kontrollerer om de to passwords er skrevet ens
        if (newPassword != confirmPassword)
        {
            // Gemmer en fejlbesked hvis de to passwords ikke er ens
            TempData["ErrorMessage"] = "The passwords do not match.";

            // Viser siden igen uden at nulstille password
            return Page();
        }

        // Sender tokenet og det nye password til AuthenticationService
        // Servicen håndterer selve nulstillingen
        string? errorMessage = _authenticationService.ResetPassword(Token, newPassword);

        // Kontrollerer om nulstillingen gav en fejl
        if (errorMessage != null)
        {
            // Gemmer fejlbeskeden fra AuthenticationService
            TempData["ErrorMessage"] = errorMessage;

            // Viser siden igen så brugeren kan prøve igen
            return Page();
        }

        // Gemmer en besked om at password er nulstillet
        TempData["SuccessMessage"] = "Your password has been reset. You can now log in.";

        // Sender brugeren til login siden
        return RedirectToPage("/Login");
    }
}
