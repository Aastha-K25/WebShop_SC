
using Microsoft.AspNetCore.Mvc; // Bruges til at håndtere sider og handlinger
using Microsoft.AspNetCore.Mvc.RazorPages; // Bruges til Razor Pages
using Obligatoris.opgave.Domain.Interfaces; // Giver adgang til vores interfaces
using Obligatoris.opgave.Domain.Models; // Giver adgang til vores modeller
using Obligatorisk_opgave.Services; // Giver adgang til vores services

namespace Obligatorisk_opgave.Pages;

// PageModel som håndterer kundens ordrehistorik
public class Orders : PageModel
{
    // Bruges til at hente kundens ordrer fra databasen
    private readonly IOrderRepository _orderRepository;

    // Bruges til at finde den bruger som er logget ind
    private readonly SessionService _sessionService;

    // Constructor som modtager vores repository og session service
    public Orders(
        IOrderRepository orderRepository,
        SessionService sessionService)
    {
        // Gemmer vores order repository
        _orderRepository = orderRepository;

        // Gemmer vores session service
        _sessionService = sessionService;
    }

    // Opretter en tom liste til kundens ordrer
    public List<Order> CustomerOrders { get; set; } = new List<Order>();

    // OnGet kører når kunden åbner siden med sine ordrer
    public IActionResult OnGet()
    {
        // Henter brugeren som er logget ind
        User? user = _sessionService.GetCurrentUser();

        // Kontrollerer om brugeren er en Customer
        if (user is not Customer customer)
        {
            // Sender brugeren til login hvis det ikke er en kunde
            return RedirectToPage("/Login");
        }

        // Henter kun ordrerne som tilhører den aktuelle kunde
        CustomerOrders = _orderRepository.GetByCustomerId(customer.Id);

        // Viser siden med kundens ordrehistorik
        return Page();
    }
}
