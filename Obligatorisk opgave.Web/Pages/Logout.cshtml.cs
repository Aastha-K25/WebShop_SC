using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Obligatorisk_opgave.Services;

namespace Obligatorisk_opgave.Pages;

public class Logout : PageModel
{
    private readonly AuthenticationService _authenticationService;

    public Logout(AuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    // Viser logout-siden
    public IActionResult OnGet()
    {
        // Hvis brugeren ikke er logget ind,
        // er der ingen session, som skal afsluttes
        if (!_authenticationService.IsAuthenticated())
        {
            return RedirectToPage("/Index");
        }

        return Page();
    }

    // Logger brugeren ud
    public IActionResult OnPost()
    {
        // Fjerner brugerens oplysninger fra sessionen
        _authenticationService.Logout();

        TempData["SuccessMessage"] =
            "You have been logged out.";

        return RedirectToPage("/Index");
    }
}