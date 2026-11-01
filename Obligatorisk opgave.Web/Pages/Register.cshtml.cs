using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Obligatorisk_opgave.Services;

namespace Obligatorisk_opgave.Pages;

public class Register : PageModel
{
    private readonly AuthenticationService _authenticationService;

    public Register(AuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    // Viser registreringssiden.
    public IActionResult OnGet()
    {
        // En bruger, der allerede er logget ind,
        // sendes tilbage til forsiden.
        if (_authenticationService.IsAuthenticated())
        {
            return RedirectToPage("/Index");
        }

        return Page();
    }

    // Opretter en ny kundekonto.
    public IActionResult OnPost()
    {
        string name = Request.Form["name"].ToString();
        string email = Request.Form["email"].ToString();
        string address = Request.Form["address"].ToString();
        string password = Request.Form["password"].ToString();
        string confirmPassword =
            Request.Form["confirmPassword"].ToString();

        // Passwordet skal skrives ens begge gange.
        if (password != confirmPassword)
        {
            TempData["ErrorMessage"] =
                "Passwords do not match.";

            return Page();
        }

        // Servicen validerer passwordet, hasher det
        // og gemmer kunden gennem repositoryet.
        string? errorMessage =
            _authenticationService.RegisterCustomer(
                name,
                email,
                password,
                address);

        if (errorMessage != null)
        {
            TempData["ErrorMessage"] = errorMessage;
            return Page();
        }

        TempData["SuccessMessage"] =
            "Your account has been created. You can now log in.";

        return RedirectToPage("/Login");
    }
}