
using Microsoft.AspNetCore.Mvc; // Bruges til at håndtere sider og handlinger
using Microsoft.AspNetCore.Mvc.RazorPages; // Bruges til Razor Pages
using Obligatoris.opgave.Domain.Interfaces; // Giver adgang til vores interfaces
using Obligatoris.opgave.Domain.Models; // Giver adgang til vores modeller
using Obligatorisk_opgave.Services; // Giver adgang til vores services

namespace Obligatorisk_opgave.Pages;

// PageModel som håndterer administration af ordrer
public class AdminOrders : PageModel
{
    // Bruges til at hente og opdatere ordrer i databasen
    private readonly IOrderRepository _orderRepository;

    // Bruges til at finde den bruger som er logget ind
    private readonly SessionService _sessionService;

    // Bruges til at kontrollere om brugeren har adgang
    private readonly AuthorizationService _authorizationService;

    // Constructor som modtager vores repository og services
    public AdminOrders(
        IOrderRepository orderRepository,
        SessionService sessionService,
        AuthorizationService authorizationService)
    {
        // Gemmer vores order repository
        _orderRepository = orderRepository;

        // Gemmer vores session service
        _sessionService = sessionService;

        // Gemmer vores authorization service
        _authorizationService = authorizationService;
    }

    // Opretter en tom liste som skal indeholde alle ordrer
    public List<Order> Orders { get; set; } = new List<Order>();

    // OnGet kører når admin åbner siden med ordrer
    public IActionResult OnGet()
    {
        // Henter brugeren som er logget ind
        User? user = _sessionService.GetCurrentUser();

        // Kontrollerer om brugeren må administrere ordrer
        if (!_authorizationService.CanManageOrders(user))
        {
            // Afviser adgang hvis brugeren ikke har tilladelse
            return Forbid();
        }

        // Henter alle ordrer fra databasen
        Orders = _orderRepository.GetAll();

        // Viser siden med alle ordrerne
        return Page();
    }

    // OnPostUpdateStatus kører når admin ændrer en ordrestatus
    public IActionResult OnPostUpdateStatus(int orderId, OrderStatus newStatus)
    {
        // Henter brugeren som er logget ind
        User? user = _sessionService.GetCurrentUser();

        // Kontrollerer om brugeren må ændre ordrer
        if (!_authorizationService.CanManageOrders(user))
        {
            // Afviser adgang hvis brugeren ikke har tilladelse
            return Forbid();
        }

        // Finder den ordre som skal opdateres
        Order? order = _orderRepository.GetById(orderId);

        // Kontrollerer om ordren findes
        if (order == null)
        {
            // Returnerer NotFound hvis ordren ikke findes
            return NotFound();
        }

        // Kontrollerer om den nye status er tilladt
        if (!CanChangeOrderStatus(order.Status, newStatus))
        {
            // Afviser ændringen hvis status ikke må skiftes
            return BadRequest();
        }

        // Gemmer den nye ordrestatus i databasen
        _orderRepository.UpdateStatus(orderId, newStatus);

        // Sender admin tilbage til siden med ordrer
        return RedirectToPage("/AdminOrders");
    }

    // Kontrollerer hvilke statusændringer der er tilladt
    // Reglerne kommer fra vores Order State Diagram
    private bool CanChangeOrderStatus(OrderStatus currentStatus, OrderStatus newStatus)
    {
        // Tjekker om ordren afventer betaling
        if (currentStatus == OrderStatus.Pending)
        {
            // Ordren må ændres til Paid eller Cancelled
            return newStatus == OrderStatus.Paid
                || newStatus == OrderStatus.Cancelled;
        }

        // Tjekker om ordren er betalt
        if (currentStatus == OrderStatus.Paid)
        {
            // Ordren må ændres til Processing eller Cancelled
            return newStatus == OrderStatus.Processing
                || newStatus == OrderStatus.Cancelled;
        }

        // Tjekker om ordren er ved at blive behandlet
        if (currentStatus == OrderStatus.Processing)
        {
            // Ordren må ændres til Shipped eller Cancelled
            return newStatus == OrderStatus.Shipped
                || newStatus == OrderStatus.Cancelled;
        }

        // Tjekker om ordren er afsendt
        if (currentStatus == OrderStatus.Shipped)
        {
            // Ordren må kun ændres til Completed
            return newStatus == OrderStatus.Completed;
        }

        // Completed og Cancelled er sluttilstande
        // Returnerer false fordi status ikke må ændres mere
        return false;
    }
}
