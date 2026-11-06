
using Microsoft.AspNetCore.Mvc; // Bruges til at håndtere sider og handlinger
using Microsoft.AspNetCore.Mvc.RazorPages; // Bruges til Razor Pages
using Obligatorisk_opgave.Services; // Giver adgang til vores services

namespace Obligatorisk_opgave.Pages;

// PageModel som håndterer logout
public class Logout : PageModel
{
    // Bruges til at kontrollere login og logge brugeren ud
    private readonly AuthenticationService _authenticationService;

    // Constructor som modtager vores AuthenticationService
    public Logout(AuthenticationService authenticationService)
    {
        // Gemmer servicen så vi kan bruge den i klassen
        _authenticationService = authenticationService;
    }

    // OnGet kører når brugeren åbner logout siden
    public IActionResult OnGet()
    {
        // Kontrollerer om brugeren er logget ind
        if (!_authenticationService.IsAuthenticated())
        {
            // Sender brugeren til forsiden hvis ingen er logget ind
            return RedirectToPage("/Index");
        }

        // Viser logout siden hvis brugeren er logget ind
        return Page();
    }

    // OnPost kører når brugeren bekræfter logout
    public IActionResult OnPost()
    {
        // Logger brugeren ud ved at afslutte login i sessionen
        _authenticationService.Logout();

        // Gemmer en besked om at brugeren er logget ud
        TempData["SuccessMessage"] = "You have been logged out.";

        // Sender brugeren tilbage til forsiden
        return RedirectToPage("/Index");
    }
}
