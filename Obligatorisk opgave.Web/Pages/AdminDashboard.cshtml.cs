
using Microsoft.AspNetCore.Mvc; // Bruges til at håndtere sider og handlinger
using Microsoft.AspNetCore.Mvc.RazorPages; // Bruges til Razor Pages
using Obligatoris.opgave.Domain.Models; // Giver adgang til vores modeller
using Obligatorisk_opgave.Services; // Giver adgang til vores services

namespace Obligatorisk_opgave.Pages;

// PageModel som håndterer admin dashboard
public class AdminDashboardModel : PageModel
{
    // Bruges til at finde den bruger som er logget ind
    private readonly SessionService _sessionService;

    // Bruges til at kontrollere om brugeren har adgang
    private readonly AuthorizationService _authorizationService;

    // Constructor som modtager de services vi skal bruge
    public AdminDashboardModel(
        SessionService sessionService,
        AuthorizationService authorizationService)
    {
        // Gemmer vores session service
        _sessionService = sessionService;

        // Gemmer vores authorization service
        _authorizationService = authorizationService;
    }

    // OnGet kører når nogen forsøger at åbne admin dashboard
    public IActionResult OnGet()
    {
        // Henter brugeren som er logget ind
        User? currentUser = _sessionService.GetCurrentUser();

        // Tjekker om der er en bruger logget ind
        if (currentUser == null)
        {
            // Sender brugeren til admin login
            return RedirectToPage("/AdminLogin");
        }

        // Tjekker om brugeren er en Admin
        if (currentUser is not Admin)
        {
            // Afviser adgang hvis brugeren ikke er admin
            return Forbid();
        }

        // Kontrollerer om brugeren har tilladelse til at administrere produkter
        if (!_authorizationService.CanManageProducts(currentUser))
        {
            // Afviser adgang hvis kontrollen fejler
            return Forbid();
        }

        // Viser admin dashboard hvis alle kontroller er godkendt
        return Page();
    }
}
