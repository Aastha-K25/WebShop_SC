
using Microsoft.AspNetCore.Mvc; // Bruges til at håndtere sider og handlinger
using Microsoft.AspNetCore.Mvc.RazorPages; // Bruges til Razor Pages
using Obligatoris.opgave.Domain.Interfaces; // Giver adgang til vores interfaces
using Obligatoris.opgave.Domain.Models; // Giver adgang til vores modeller
using Obligatorisk_opgave.Services; // Giver adgang til vores services

namespace Obligatorisk_opgave.Pages;

// PageModel som håndterer administration af kunder
public class AdminUsers : PageModel
{
    // Bruges til at hente og opdatere brugere i databasen
    private readonly IUserRepository _userRepository;

    // Bruges til at finde den bruger som er logget ind
    private readonly SessionService _sessionService;

    // Bruges til at kontrollere om brugeren har adgang
    private readonly AuthorizationService _authorizationService;

    // Constructor som modtager vores repository og services
    public AdminUsers(
        IUserRepository userRepository,
        SessionService sessionService,
        AuthorizationService authorizationService)
    {
        // Gemmer vores user repository
        _userRepository = userRepository;

        // Gemmer vores session service
        _sessionService = sessionService;

        // Gemmer vores authorization service
        _authorizationService = authorizationService;
    }

    // Opretter en tom liste som kun skal indeholde Customer konti
    public List<Customer> Customers { get; set; } = new List<Customer>();

    // OnGet kører når admin åbner siden med kunder
    public IActionResult OnGet()
    {
        // Henter brugeren som er logget ind
        User? user = _sessionService.GetCurrentUser();

        // Kontrollerer om brugeren må administrere andre brugere
        if (!_authorizationService.CanManageUsers(user))
        {
            // Afviser adgang hvis brugeren ikke har tilladelse
            return Forbid();
        }

        // Henter alle brugere fra databasen
        List<User> users = _userRepository.GetAll();

        // Filtrerer listen så vi kun får Customer objekter
        // OfType vælger kun brugere som er Customer
        // ToList laver resultatet om til en liste
        Customers = users.OfType<Customer>().ToList();

        // Viser siden med alle kunderne
        return Page();
    }

    // OnPostDeactivate kører når admin deaktiverer en kunde
    public IActionResult OnPostDeactivate(int id)
    {
        // Henter brugeren som er logget ind
        User? user = _sessionService.GetCurrentUser();

        // Kontrollerer om brugeren må administrere kunder
        if (!_authorizationService.CanManageUsers(user))
        {
            // Afviser adgang hvis brugeren ikke har tilladelse
            return Forbid();
        }

        // Finder den bruger som skal deaktiveres
        User? targetUser = _userRepository.GetById(id);

        // Kontrollerer at brugeren er en Customer
        // Admin konti må ikke deaktiveres gennem denne side
        if (targetUser is not Customer)
        {
            // Returnerer NotFound hvis brugeren ikke er en kunde
            return NotFound();
        }

        // Deaktiverer kunden i databasen
        // Kunden bliver ikke slettet
        _userRepository.Deactivate(id);

        // Sender admin tilbage til siden med kunder
        return RedirectToPage("/AdminUsers");
    }

    // OnPostActivate kører når admin aktiverer en kunde igen
    public IActionResult OnPostActivate(int id)
    {
        // Henter brugeren som er logget ind
        User? user = _sessionService.GetCurrentUser();

        // Kontrollerer om brugeren må administrere kunder
        if (!_authorizationService.CanManageUsers(user))
        {
            // Afviser adgang hvis brugeren ikke har tilladelse
            return Forbid();
        }

        // Finder den bruger som skal aktiveres
        User? targetUser = _userRepository.GetById(id);

        // Kontrollerer at brugeren er en Customer
        // Gemmer samtidig kunden i variablen customer
        if (targetUser is not Customer customer)
        {
            // Returnerer NotFound hvis brugeren ikke er en kunde
            return NotFound();
        }

        // Ændrer kundens status til aktiv
        customer.IsActive = true;

        // Gemmer ændringen i databasen
        _userRepository.Update(customer);

        // Sender admin tilbage til siden med kunder
        return RedirectToPage("/AdminUsers");
    }
}
