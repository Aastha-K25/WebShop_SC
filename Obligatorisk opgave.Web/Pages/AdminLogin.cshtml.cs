
using Microsoft.AspNetCore.Mvc; // Bruges til at håndtere sider og handlinger
using Microsoft.AspNetCore.Mvc.RazorPages; // Bruges til Razor Pages
using Obligatoris.opgave.Domain.Models; // Giver adgang til vores modeller
using Obligatorisk_opgave.Services; // Giver adgang til vores services

namespace Obligatorisk_opgave.Pages;

// PageModel som håndterer login for admin
public class AdminLoginModel : PageModel
{
    // Bruges til at kontrollere login og finde den aktuelle bruger
    private readonly AuthenticationService _authenticationService;

    // Constructor som modtager vores AuthenticationService
    public AdminLoginModel(AuthenticationService authenticationService)
    {
        // Gemmer servicen så vi kan bruge den i klassen
        _authenticationService = authenticationService;
    }

    // Binder email til loginformularen
    [BindProperty]
    public string Email { get; set; } = "";

    // Binder password til loginformularen
    [BindProperty]
    public string Password { get; set; } = "";

    // Gemmer en eventuel fejlbesked som kan vises på siden
    public string? ErrorMessage { get; set; }

    // OnGet kører når brugeren åbner admin login siden
    public IActionResult OnGet()
    {
        // Henter brugeren som allerede er logget ind
        User? currentUser = _authenticationService.GetCurrentUser();

        // Tjekker om brugeren allerede er en Admin
        if (currentUser is Admin)
        {
            // Sender admin direkte til dashboard
            return Redirect("/AdminDashboard");
        }

        // Viser login siden hvis brugeren ikke allerede er admin
        return Page();
    }

    // OnPost kører når brugeren forsøger at logge ind
    public IActionResult OnPost()
    {
        // Sender email og password til vores login service
        // Login metoden kontrollerer oplysningerne
        string? loginError = _authenticationService.Login(Email, Password);

        // Kontrollerer om login gav en fejl
        if (loginError != null)
        {
            // Gemmer fejlbeskeden så den kan vises på siden
            ErrorMessage = loginError;

            // Viser login siden igen
            return Page();
        }

        // Henter brugeren efter et vellykket login
        User? currentUser = _authenticationService.GetCurrentUser();

        // Kontrollerer om den indloggede bruger er en Admin
        if (currentUser is not Admin)
        {
            // Logger brugeren ud hvis det ikke er en admin
            _authenticationService.Logout();

            // Viser en fejlbesked om ugyldigt admin login
            ErrorMessage = "Invalid admin email or password.";

            // Viser login siden igen
            return Page();
        }

        // Sender admin til dashboard når login er godkendt
        return Redirect("/AdminDashboard");
    }
}
