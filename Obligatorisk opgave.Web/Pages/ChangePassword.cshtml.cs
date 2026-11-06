
using Microsoft.AspNetCore.Mvc; // Bruges til at håndtere sider og handlinger
using Microsoft.AspNetCore.Mvc.RazorPages; // Bruges til Razor Pages
using Obligatoris.opgave.Domain.Models; // Giver adgang til vores modeller
using Obligatorisk_opgave.Services; // Giver adgang til vores services

namespace Obligatorisk_opgave.Pages;

// PageModel som håndterer ændring af password
public class ChangePassword : PageModel
{
    // Bruges til at kontrollere brugeren og ændre password
    private readonly AuthenticationService _authenticationService;

    // Constructor som modtager vores AuthenticationService
    public ChangePassword(AuthenticationService authenticationService)
    {
        // Gemmer servicen så vi kan bruge den i klassen
        _authenticationService = authenticationService;
    }

    // OnGet kører når kunden åbner siden til at ændre password
    public IActionResult OnGet()
    {
        // Henter brugeren som er logget ind
        User? currentUser = _authenticationService.GetCurrentUser();

        // Kontrollerer om brugeren er en Customer
        if (currentUser is not Customer)
        {
            // Sender brugeren til login hvis det ikke er en kunde
            return RedirectToPage("/Login");
        }

        // Viser siden hvor kunden kan ændre password
        return Page();
    }

    // OnPost kører når kunden sender formularen
    public IActionResult OnPost()
    {
        // Henter brugeren som er logget ind
        User? currentUser = _authenticationService.GetCurrentUser();

        // Kontrollerer igen at brugeren er en Customer
        if (currentUser is not Customer)
        {
            // Sender brugeren til login hvis det ikke er en kunde
            return RedirectToPage("/Login");
        }

        // Henter kundens nuværende password fra formularen
        string currentPassword = Request.Form["currentPassword"].ToString();

        // Henter det nye password fra formularen
        string newPassword = Request.Form["newPassword"].ToString();

        // Henter bekræftelsen af det nye password
        string confirmPassword = Request.Form["confirmPassword"].ToString();

        // Kontrollerer om et af felterne er tomme
        if (string.IsNullOrWhiteSpace(currentPassword) ||
            string.IsNullOrWhiteSpace(newPassword) ||
            string.IsNullOrWhiteSpace(confirmPassword))
        {
            // Gemmer en fejlbesked som kan vises på siden
            TempData["ErrorMessage"] = "Please fill out all fields.";

            // Viser siden igen så kunden kan udfylde felterne
            return Page();
        }

        // Kontrollerer om det nye password er skrevet ens begge gange
        if (newPassword != confirmPassword)
        {
            // Viser en fejl hvis de to passwords ikke er ens
            TempData["ErrorMessage"] = "The new passwords do not match.";

            // Viser siden igen uden at ændre password
            return Page();
        }

        // Sender det nuværende og nye password til AuthenticationService
        // Servicen forsøger at ændre password og returnerer en eventuel fejl
        string? errorMessage = _authenticationService.ChangePassword(
            currentPassword, newPassword);

        // Kontrollerer om der opstod en fejl under ændringen
        if (errorMessage != null)
        {
            // Gemmer fejlbeskeden så den kan vises
            TempData["ErrorMessage"] = errorMessage;

            // Viser siden igen hvis ændringen fejlede
            return Page();
        }

        // Logger kunden ud efter password er ændret
        _authenticationService.Logout();

        // Gemmer en besked om at password er ændret
        TempData["SuccessMessage"] = "Your password has been changed. Please log in again.";

        // Sender kunden til login så det nye password kan bruges
        return RedirectToPage("/Login");
    }
}
