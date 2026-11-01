using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Obligatorisk_opgave.Services;

namespace Obligatorisk_opgave.Pages;

public class Login : PageModel
{
    private readonly AuthenticationService _authenticationService;

    public Login(AuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    // Kører når login-siden åbnes
    public IActionResult OnGet()
    {
        // En bruger, der allerede er logget ind,
        // sendes tilbage til forsiden
        if (_authenticationService.IsAuthenticated())
        {
            return RedirectToPage("/Index");
        }

        return Page();
    }

    // Kører når login-formularen sendes
    public IActionResult OnPost()
    {
        string email =
            Request.Form["email"].ToString();

        string password =
            Request.Form["password"].ToString();

        // AuthenticationService finder brugeren,
        // kontrollerer passwordet og opretter sessionen
        string? errorMessage =
            _authenticationService.Login(
                email,
                password);

        // Login mislykkedes
        if (errorMessage != null)
        {
            TempData["ErrorMessage"] = errorMessage;
            return Page();
        }

        // Login lykkedes
        return RedirectToPage("/Index");
    }
}