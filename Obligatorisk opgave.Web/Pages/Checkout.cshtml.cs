
using Microsoft.AspNetCore.Mvc; // Bruges til at håndtere sider og handlinger
using Microsoft.AspNetCore.Mvc.RazorPages; // Bruges til Razor Pages
using Obligatoris.opgave.Domain.Interfaces; // Giver adgang til vores interfaces
using Obligatoris.opgave.Domain.Models; // Giver adgang til vores modeller
using Obligatorisk_opgave.Services; // Giver adgang til vores services

namespace Obligatorisk_opgave.Pages;

// PageModel som håndterer checkout og oprettelse af ordrer
public class Checkout : PageModel
{
    // Bruges til at hente kundens indkøbskurv fra databasen
    private readonly IShoppingCartRepository _shoppingCartRepository;

    // Bruges til at hente produkter fra databasen
    private readonly IProductRepository _productRepository;

    // Bruges til at oprette ordrer i databasen
    private readonly IOrderRepository _orderRepository;

    // Bruges til at finde den bruger som er logget ind
    private readonly SessionService _sessionService;

    // Bruges til at kontrollere om brugeren har adgang
    private readonly AuthorizationService _authorizationService;

    // Constructor som modtager vores repositories og services
    public Checkout(
        IShoppingCartRepository shoppingCartRepository,
        IProductRepository productRepository,
        IOrderRepository orderRepository,
        SessionService sessionService,
        AuthorizationService authorizationService)
    {
        // Gemmer vores shopping cart repository
        _shoppingCartRepository = shoppingCartRepository;

        // Gemmer vores product repository
        _productRepository = productRepository;

        // Gemmer vores order repository
        _orderRepository = orderRepository;

        // Gemmer vores session service
        _sessionService = sessionService;

        // Gemmer vores authorization service
        _authorizationService = authorizationService;
    }

    // Gemmer kunden som gennemfører checkout
    public Customer? Customer { get; set; }

    // Gemmer kundens indkøbskurv fra databasen
    public ShoppingCart? ShoppingCart { get; set; }

    // Opretter en tom liste til produkterne i kurven
    public List<Product> Products { get; set; } = new List<Product>();

    // Fast leveringspris på 49 kr
    public decimal ShippingPrice { get; set; } = 49;

    // OnGet kører når kunden åbner checkout siden
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

        // Kontrollerer om brugeren har tilladelse til checkout
        // Bruger vores AuthorizationService og adgangsregler
        if (!_authorizationService.CanCheckout(user))
        {
            // Afviser adgang hvis brugeren ikke har tilladelse
            return Forbid();
        }

        // Gemmer den aktuelle kunde
        Customer = customer;

        // Henter kundens id
        int customerId = customer.Id;

        // Finder kundens indkøbskurv i databasen
        ShoppingCart = _shoppingCartRepository.GetByCustomerId(customerId);

        // Kontrollerer om kunden har en indkøbskurv
        if (ShoppingCart == null)
        {
            // Sender kunden tilbage hvis kurven ikke findes
            return RedirectToPage("/Cart");
        }

        // Kontrollerer om indkøbskurven faktisk tilhører kunden
        if (!_authorizationService.OwnsCart(customer, ShoppingCart))
        {
            // Afviser adgang hvis kunden ikke ejer kurven
            return Forbid();
        }

        // Henter alle produkterne fra kundens indkøbskurv
        Products = _shoppingCartRepository.GetProducts(ShoppingCart.Id);

        // Kontrollerer om indkøbskurven er tom
        if (Products.Count == 0)
        {
            // Sender kunden tilbage hvis der ikke er nogen produkter
            return RedirectToPage("/Cart");
        }

        // Viser checkout siden med kundens produkter
        return Page();
    }

    // OnPost kører når kunden forsøger at gennemføre demo betalingen
    public IActionResult OnPost(bool confirmDemoPayment)
    {
        // Henter brugeren som er logget ind
        User? user = _sessionService.GetCurrentUser();

        // Kontrollerer om brugeren er en Customer
        if (user is not Customer customer)
        {
            // Sender brugeren til login hvis det ikke er en kunde
            return RedirectToPage("/Login");
        }

        // Kontrollerer om brugeren har tilladelse til checkout
        if (!_authorizationService.CanCheckout(user))
        {
            // Afviser adgang hvis brugeren ikke har tilladelse
            return Forbid();
        }

        // Kontrollerer om kunden har bekræftet demo betalingen
        // Der bliver ikke brugt rigtige betalingsoplysninger
        if (!confirmDemoPayment)
        {
            // Gemmer en fejlbesked hvis betalingen ikke er bekræftet
            TempData["ErrorMessage"] = "Please confirm the demo payment.";

            // Sender kunden tilbage til checkout
            return RedirectToPage("/Checkout");
        }

        // Henter kundens id
        int customerId = customer.Id;

        // Finder kundens indkøbskurv i databasen
        ShoppingCart? shoppingCart = _shoppingCartRepository.GetByCustomerId(customerId);

        // Kontrollerer om indkøbskurven findes
        if (shoppingCart == null)
        {
            // Sender kunden tilbage hvis kurven ikke findes
            return RedirectToPage("/Cart");
        }

        // Kontrollerer om kunden faktisk ejer indkøbskurven
        if (!_authorizationService.OwnsCart(customer, shoppingCart))
        {
            // Afviser adgang hvis kurven ikke tilhører kunden
            return Forbid();
        }

        // Forsøger at gennemføre købet
        try
        {
            // Opretter ordren gennem vores OrderRepository
            // PlaceOrder opdaterer også lageret og tømmer kurven
            // Databaseændringerne sker i en samlet transaction
            int orderId = _orderRepository.PlaceOrder(customerId, ShippingPrice);

            // Sender kunden til ordrebekræftelsen med det nye ordre id
            return RedirectToPage("/OrderConfirmation", new { id = orderId });
        }
        // Fanger InvalidOperationException hvis checkout ikke kan gennemføres
        catch (InvalidOperationException)
        {
            // Gemmer en fejlbesked som kan vises til kunden
            TempData["ErrorMessage"] = "Your order could not be completed. Please check your cart.";

            // Sender kunden tilbage til indkøbskurven
            return RedirectToPage("/Cart");
        }
    }

    // Hjælpemetode som henter antallet af et bestemt produkt
    public int GetQuantity(int productId)
    {
        // Kontrollerer om kundens indkøbskurv findes
        if (ShoppingCart == null)
        {
            // Returnerer nul hvis kurven ikke findes
            return 0;
        }

        // Finder antallet af produktet i databasen
        return _shoppingCartRepository.GetProductQuantity(ShoppingCart.Id, productId);
    }

    // Hjælpemetode som beregner den samlede pris for et produkt
    public decimal GetProductTotal(Product product)
    {
        // Finder hvor mange kunden har af produktet
        int quantity = GetQuantity(product.Id);

        // Ganger produktets pris med antallet
        return product.Price * quantity;
    }

    // Hjælpemetode som beregner den samlede pris uden fragt
    public decimal GetSubtotal()
    {
        // Starter subtotalen på nul
        decimal subtotal = 0;

        // Går igennem alle produkterne i indkøbskurven
        foreach (Product product in Products)
        {
            // Beregner den samlede pris for det enkelte produkt
            decimal productTotal = GetProductTotal(product);

            // Lægger produktets samlede pris til subtotalen
            subtotal = subtotal + productTotal;
        }

        // Returnerer den samlede pris uden fragt
        return subtotal;
    }

    // Hjælpemetode som beregner den samlede pris inklusive fragt
    public decimal GetTotalPrice()
    {
        // Lægger den faste fragtpris til subtotalen
        return GetSubtotal() + ShippingPrice;
    }
}
