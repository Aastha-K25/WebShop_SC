using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Obligatorisk_opgave.Services;

namespace Obligatorisk_opgave.Pages;

public class Register : PageModel
{
    private readonly AuthenticationService _authenticationService;


    public Register(
        AuthenticationService authenticationService)
    {
        _authenticationService =
            authenticationService;
    }


    // Viser registreringssiden
    public IActionResult OnGet()
    {
        // Hvis der allerede er en bruger logget ind,
        // skal der ikke kunne oprettes en ny konto
        // i den samme session.
        if (_authenticationService.IsAuthenticated())
        {
            return RedirectToPage("/Index");
        }


        return Page();
    }


    // Opretter en ny kundekonto
    public IActionResult OnPost()
    {
        string name =
            Request.Form["name"]
                .ToString()
                .Trim();


        string surname =
            Request.Form["surname"]
                .ToString()
                .Trim();


        string email =
            Request.Form["email"]
                .ToString()
                .Trim()
                .ToLowerInvariant();


        string address =
            Request.Form["address"]
                .ToString()
                .Trim();


        string password =
            Request.Form["password"]
                .ToString();


        string confirmPassword =
            Request.Form["confirmPassword"]
                .ToString();


        // Alle felter skal være udfyldt
        if (string.IsNullOrWhiteSpace(name) ||
            string.IsNullOrWhiteSpace(surname) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(address) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(confirmPassword))
        {
            TempData["ErrorMessage"] =
                "Please fill out all fields.";

            return Page();
        }


        // Password skal være skrevet ens begge gange
        if (password != confirmPassword)
        {
            TempData["ErrorMessage"] =
                "Passwords do not match.";

            return Page();
        }


        // AuthenticationService:
        // - kontrollerer om email allerede findes
        // - validerer password
        // - hasher password
        // - gemmer kunden i databasen
        string? errorMessage =
            _authenticationService.RegisterCustomer(
                name,
                surname,
                email,
                password,
                address);


        if (errorMessage != null)
        {
            TempData["ErrorMessage"] =
                errorMessage;

            return Page();
        }


        TempData["SuccessMessage"] =
            "Your account has been created. You can now log in.";


        return RedirectToPage("/Login");
    }
}