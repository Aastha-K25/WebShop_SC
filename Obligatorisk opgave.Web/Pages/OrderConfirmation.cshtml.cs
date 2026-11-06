
using Microsoft.AspNetCore.Mvc; // Bruges til at håndtere sider og handlinger
using Microsoft.AspNetCore.Mvc.RazorPages; // Bruges til Razor Pages
using Obligatoris.opgave.Domain.Interfaces; // Giver adgang til vores interfaces
using Obligatoris.opgave.Domain.Models; // Giver adgang til vores modeller
using Obligatorisk_opgave.Services; // Giver adgang til vores services

namespace Obligatorisk_opgave.Pages;

// PageModel som håndterer ordrebekræftelsen
public class OrderConfirmation : PageModel
{
    // Bruges til at hente ordrer og ordreprodukter fra databasen
    private readonly IOrderRepository _orderRepository;

    // Bruges til at hente produktoplysninger fra databasen
    private readonly IProductRepository _productRepository;

    // Bruges til at finde den bruger som er logget ind
    private readonly SessionService _sessionService;

    // Bruges til at kontrollere om brugeren må se ordren
    private readonly AuthorizationService _authorizationService;

    // Constructor som modtager vores repositories og services
    public OrderConfirmation(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        SessionService sessionService,
        AuthorizationService authorizationService)
    {
        // Gemmer vores order repository
        _orderRepository = orderRepository;

        // Gemmer vores product repository
        _productRepository = productRepository;

        // Gemmer vores session service
        _sessionService = sessionService;

        // Gemmer vores authorization service
        _authorizationService = authorizationService;
    }

    // Gemmer den ordre som skal vises på siden
    public Order? Order { get; set; }

    // Opretter en tom liste til produkterne på ordren
    public List<OrderProduct> OrderProducts { get; set; } = new List<OrderProduct>();

    // Gemmer produktoplysninger med produktets id som nøgle
    // Dictionary gør at vi kan finde et produkt direkte ud fra dets id
    public Dictionary<int, Product> Products { get; set; } = new Dictionary<int, Product>();

    // Bruges til at se om ordren vises fra ordrehistorikken
    public bool IsHistory { get; set; }

    // OnGet kører når brugeren åbner ordrebekræftelsen
    // id fortæller hvilken ordre der skal vises
    // history er som standard false
    public IActionResult OnGet(int id, bool history = false)
    {
        // Henter brugeren som er logget ind
        User? user = _sessionService.GetCurrentUser();

        // Kontrollerer om der er en bruger logget ind
        if (user == null)
        {
            // Sender brugeren til login hvis ingen er logget ind
            return RedirectToPage("/Login");
        }

        // Finder ordren i databasen ud fra dens id
        Order? order = _orderRepository.GetById(id);

        // Kontrollerer om ordren findes
        if (order == null)
        {
            // Returnerer NotFound hvis ordren ikke findes
            return NotFound();
        }

        // Kontrollerer om brugeren har tilladelse til at se ordren
        // AuthorizationService bruger vores adgangsregler
        bool canViewOrder = _authorizationService.CanViewOrder(user, order);

        // Tjekker om brugeren har adgang til ordren
        if (!canViewOrder)
        {
            // Afviser adgang hvis brugeren ikke må se ordren
            return Forbid();
        }

        // Gemmer ordren så den kan vises på siden
        Order = order;

        // Gemmer om ordren vises som en del af ordrehistorikken
        IsHistory = history;

        // Henter alle produkterne som hører til ordren
        OrderProducts = _orderRepository.GetOrderProducts(order.Id);

        // Går igennem alle produkterne på ordren
        foreach (OrderProduct orderProduct in OrderProducts)
        {
            // Finder produktets oplysninger i databasen
            Product? product = _productRepository.GetById(orderProduct.ProductId);

            // Kontrollerer om produktet stadig findes
            if (product != null)
            {
                // Gemmer produktet i vores Dictionary med produktets id som nøgle
                Products[product.Id] = product;
            }
        }

        // Viser ordrebekræftelsen med ordren og dens produkter
        return Page();
    }
}
