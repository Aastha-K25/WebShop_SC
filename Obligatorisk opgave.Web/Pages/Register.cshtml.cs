
using Microsoft.AspNetCore.Mvc; // Bruges til at håndtere sider og handlinger
using Microsoft.AspNetCore.Mvc.RazorPages; // Bruges til Razor Pages
using Obligatorisk_opgave.Services; // Giver adgang til vores services

namespace Obligatorisk_opgave.Pages;

// PageModel som håndterer oprettelse af en ny kundekonto
public class Register : PageModel
{
    // Bruges til at registrere kunder og kontrollere login
    private readonly AuthenticationService _authenticationService;

    // Constructor som modtager vores AuthenticationService
    public Register(AuthenticationService authenticationService)
    {
        // Gemmer servicen så vi kan bruge den i klassen
        _authenticationService = authenticationService;
    }

    // OnGet kører når brugeren åbner registreringssiden
    public IActionResult OnGet()
    {
        // Kontrollerer om der allerede er en bruger logget ind
        // Vi tillader ikke registrering i samme session hvis nogen er logget ind
        if (_authenticationService.IsAuthenticated())
        {
            // Sender brugeren til forsiden hvis de allerede er logget ind
            return RedirectToPage("/Index");
        }

        // Viser registreringssiden hvis ingen er logget ind
        return Page();
    }

    // OnPost kører når brugeren sender registreringsformularen
    public IActionResult OnPost()
    {
        // Henter navnet fra formularen og fjerner ekstra mellemrum
        string name = Request.Form["name"].ToString().Trim();

        // Henter efternavnet og fjerner ekstra mellemrum
        string surname = Request.Form["surname"].ToString().Trim();

        // Henter email og laver den om til små bogstaver
        string email = Request.Form["email"].ToString().Trim().ToLowerInvariant();

        // Henter adressen og fjerner ekstra mellemrum
        string address = Request.Form["address"].ToString().Trim();

        // Henter password uden at ændre det
        string password = Request.Form["password"].ToString();

        // Henter bekræftelsen af password
        string confirmPassword = Request.Form["confirmPassword"].ToString();

        // Kontrollerer om et af felterne er tomme
        if (string.IsNullOrWhiteSpace(name) ||
            string.IsNullOrWhiteSpace(surname) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(address) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(confirmPassword))
        {
            // Gemmer en fejlbesked hvis et felt mangler
            TempData["ErrorMessage"] = "Please fill out all fields.";

            // Viser registreringssiden igen
            return Page();
        }

        // Kontrollerer om password er skrevet ens begge gange
        if (password != confirmPassword)
        {
            // Gemmer en fejlbesked hvis de to passwords ikke er ens
            TempData["ErrorMessage"] = "Passwords do not match.";

            // Viser siden igen så brugeren kan rette password
            return Page();
        }

        // Sender brugerens oplysninger til AuthenticationService
        // Servicen kontrollerer email og password
        // Servicen hasher password og gemmer kunden i databasen
        string? errorMessage = _authenticationService.RegisterCustomer(
            name, surname, email, password, address);

        // Kontrollerer om registreringen gav en fejl
        if (errorMessage != null)
        {
            // Gemmer fejlbeskeden fra AuthenticationService
            TempData["ErrorMessage"] = errorMessage;

            // Viser registreringssiden igen
            return Page();
        }

        // Gemmer en besked om at kontoen er oprettet
        TempData["SuccessMessage"] = "Your account has been created. You can now log in.";

        // Sender brugeren til login siden
        return RedirectToPage("/Login");
    }
}
