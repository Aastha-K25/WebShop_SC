
using Microsoft.AspNetCore.Mvc; // Bruges til at håndtere sider og handlinger
using Microsoft.AspNetCore.Mvc.RazorPages; // Bruges til Razor Pages
using Obligatoris.opgave.Domain.Interfaces; // Giver adgang til vores interfaces
using Obligatoris.opgave.Domain.Models; // Giver adgang til vores modeller
using Obligatorisk_opgave.Services; // Giver adgang til vores services

namespace Obligatorisk_opgave.Pages;

// PageModel som håndterer kundens kontoside
public class Account : PageModel
{
    // Bruges til at hente og opdatere brugere i databasen
    private readonly IUserRepository _userRepository;

    // Bruges til at finde den bruger som er logget ind
    private readonly SessionService _sessionService;

    // Bruges til at kontrollere om brugeren har adgang
    private readonly AuthorizationService _authorizationService;

    // Constructor som modtager vores repositories og services
    public Account(
        IUserRepository userRepository,
        SessionService sessionService,
        AuthorizationService authorizationService)
    {
        // Gemmer vores repository så vi kan bruge det i klassen
        _userRepository = userRepository;

        // Gemmer vores session service
        _sessionService = sessionService;

        // Gemmer vores authorization service
        _authorizationService = authorizationService;
    }

    // Binder navnet til formularen på hjemmesiden
    [BindProperty]
    public string Name { get; set; } = "";

    // Binder efternavnet til formularen
    [BindProperty]
    public string Surname { get; set; } = "";

    // Binder email til formularen
    [BindProperty]
    public string Email { get; set; } = "";

    // Binder adressen til formularen
    [BindProperty]
    public string Address { get; set; } = "";

    // OnGet kører når kunden åbner kontosiden
    public IActionResult OnGet()
    {
        // Henter brugeren som er logget ind
        User? user = _sessionService.GetCurrentUser();

        // Tjekker om brugeren er en Customer
        if (user is not Customer customer)
        {
            // Sender brugeren til login hvis det ikke er en kunde
            return RedirectToPage("/Login");
        }

        // Kontrollerer om kunden har adgang til kontoen
        if (!_authorizationService.OwnsAccount(customer, customer.Id))
        {
            // Afviser adgang hvis kontrollen fejler
            return Forbid();
        }

        // Henter kundens navn
        Name = customer.Name;

        // Henter kundens efternavn
        Surname = customer.Surname;

        // Henter kundens email
        Email = customer.Email;

        // Henter kundens adresse
        Address = customer.Address;

        // Viser kontosiden med kundens oplysninger
        return Page();
    }

    // OnPost kører når kunden gemmer ændringer i formularen
    public IActionResult OnPost()
    {
        // Henter brugeren som er logget ind
        User? user = _sessionService.GetCurrentUser();

        // Tjekker om brugeren er en Customer
        if (user is not Customer customer)
        {
            // Sender brugeren til login hvis det ikke er en kunde
            return RedirectToPage("/Login");
        }

        // Kontrollerer om kunden har adgang til kontoen
        if (!_authorizationService.OwnsAccount(customer, customer.Id))
        {
            // Afviser adgang hvis kontrollen fejler
            return Forbid();
        }

        // Fjerner ekstra mellemrum fra navnet
        Name = Name.Trim();

        // Fjerner ekstra mellemrum fra efternavnet
        Surname = Surname.Trim();

        // Fjerner mellemrum og laver email om til små bogstaver
        Email = Email.Trim().ToLowerInvariant();

        // Fjerner ekstra mellemrum fra adressen
        Address = Address.Trim();

        // Kontrollerer om et af felterne er tomme
        if (string.IsNullOrWhiteSpace(Name) ||
            string.IsNullOrWhiteSpace(Surname) ||
            string.IsNullOrWhiteSpace(Email) ||
            string.IsNullOrWhiteSpace(Address))
        {
            // Tilføjer en fejlbesked til formularen
            ModelState.AddModelError("", "Please fill out all fields.");

            // Viser siden igen så kunden kan rette oplysningerne
            return Page();
        }

        // Tjekker om kunden har ændret sin email
        // Kontrollerer også om den nye email allerede findes
        if (!string.Equals(customer.Email, Email, StringComparison.OrdinalIgnoreCase) &&
            _userRepository.EmailExists(Email))
        {
            // Viser en fejl hvis email allerede bruges af en anden
            ModelState.AddModelError("", "An account with this email already exists.");

            // Viser siden igen uden at gemme ændringerne
            return Page();
        }

        // Opdaterer kundens navn
        customer.Name = Name;

        // Opdaterer kundens efternavn
        customer.Surname = Surname;

        // Opdaterer kundens email
        customer.Email = Email;

        // Opdaterer kundens adresse
        customer.Address = Address;

        // Gemmer de nye oplysninger i databasen
        _userRepository.Update(customer);

        // Sender kunden tilbage til kontosiden
        return RedirectToPage("/Account");
    }
}
