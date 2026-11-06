
using Microsoft.AspNetCore.Mvc; // Bruges til at håndtere sider og handlinger
using Microsoft.AspNetCore.Mvc.RazorPages; // Bruges til Razor Pages
using Obligatoris.opgave.Domain.Interfaces; // Giver adgang til vores interfaces
using Obligatoris.opgave.Domain.Models; // Giver adgang til vores modeller
using Obligatorisk_opgave.Services; // Giver adgang til vores services

namespace Obligatorisk_opgave.Pages;

// PageModel som håndterer indkøbskurven
public class Cart : PageModel
{
    // Bruges til at hente og ændre indkøbskurven i databasen
    private readonly IShoppingCartRepository _shoppingCartRepository;

    // Bruges til at hente produkter fra databasen
    private readonly IProductRepository _productRepository;

    // Bruges til at finde brugeren og håndtere gæstekurven
    private readonly SessionService _sessionService;

    // Constructor som modtager vores repositories og session service
    public Cart(
        IShoppingCartRepository shoppingCartRepository,
        IProductRepository productRepository,
        SessionService sessionService)
    {
        // Gemmer vores shopping cart repository
        _shoppingCartRepository = shoppingCartRepository;

        // Gemmer vores product repository
        _productRepository = productRepository;

        // Gemmer vores session service
        _sessionService = sessionService;
    }

    // Gemmer kundens indkøbskurv fra databasen
    public ShoppingCart? ShoppingCart { get; set; }

    // Opretter en tom liste til produkterne i kurven
    public List<Product> Products { get; set; } = new List<Product>();

    // Bruges til at kontrollere om kurven tilhører en gæst
    public bool IsGuest { get; set; }

    // Fast leveringspris på 49 kr
    public decimal ShippingPrice { get; set; } = 49;

    // OnGet kører når indkøbskurven åbnes
    public void OnGet()
    {
        // Henter den rigtige kurv ud fra brugertypen
        LoadCart();
    }

    // OnPost kører når kunden ændrer noget i indkøbskurven
    public IActionResult OnPost(string action, int productId, int quantity)
    {
        // Henter brugeren som er logget ind
        User? user = _sessionService.GetCurrentUser();

        // Gæster har deres indkøbskurv gemt i session
        if (user == null)
        {
            // Kontrollerer om gæsten vil fjerne et produkt
            if (action == "remove")
            {
                // Fjerner produktet fra gæstekurven
                _sessionService.RemoveFromGuestCart(productId);

                // Sender gæsten tilbage til kurven
                return RedirectToPage("/Cart");
            }

            // Kontrollerer om gæsten vil tømme hele kurven
            if (action == "clear")
            {
                // Fjerner alle produkter fra gæstekurven
                _sessionService.ClearGuestCart();

                // Sender gæsten tilbage til kurven
                return RedirectToPage("/Cart");
            }

            // Kontrollerer om gæsten vil ændre antal
            if (action == "update")
            {
                // Tjekker om det nye antal er nul eller mindre
                if (quantity <= 0)
                {
                    // Fjerner produktet hvis antal er nul eller mindre
                    _sessionService.RemoveFromGuestCart(productId);

                    // Sender gæsten tilbage til kurven
                    return RedirectToPage("/Cart");
                }

                // Finder produktet i databasen
                Product? product = _productRepository.GetById(productId);

                // Kontrollerer om produktet findes
                if (product == null)
                {
                    // Sender gæsten tilbage hvis produktet ikke findes
                    return RedirectToPage("/Cart");
                }

                // Kontrollerer om antal overstiger lageret
                if (quantity > product.StockQuantity)
                {
                    // Ændrer antal så det højst svarer til lageret
                    quantity = product.StockQuantity;
                }

                // Gemmer det nye antal i gæstekurven
                _sessionService.UpdateGuestCartQuantity(productId, quantity);
            }

            // Sender gæsten tilbage til indkøbskurven
            return RedirectToPage("/Cart");
        }

        // Admin må ikke bruge kundernes indkøbskurv
        if (user is Admin)
        {
            // Sender admin til produktsiden
            return RedirectToPage("/Products");
        }

        // Kontrollerer om brugeren er en Customer
        if (user is not Customer customer)
        {
            // Sender andre brugertyper til produktsiden
            return RedirectToPage("/Products");
        }

        // Henter kundens id
        int customerId = customer.Id;

        // Finder kundens indkøbskurv i databasen
        ShoppingCart? shoppingCart = _shoppingCartRepository.GetByCustomerId(customerId);

        // Kontrollerer om kunden har en kurv
        if (shoppingCart == null)
        {
            // Sender kunden tilbage hvis kurven ikke findes
            return RedirectToPage("/Cart");
        }

        // Kontrollerer om kunden vil fjerne et produkt
        if (action == "remove")
        {
            // Fjerner produktet fra kundens indkøbskurv
            _shoppingCartRepository.RemoveProduct(shoppingCart.Id, productId);

            // Sender kunden tilbage til kurven
            return RedirectToPage("/Cart");
        }

        // Kontrollerer om kunden vil tømme hele kurven
        if (action == "clear")
        {
            // Fjerner alle produkter fra kundens kurv
            _shoppingCartRepository.Clear(shoppingCart.Id);

            // Sender kunden tilbage til kurven
            return RedirectToPage("/Cart");
        }

        // Kontrollerer om kunden vil ændre antal
        if (action == "update")
        {
            // Tjekker om det nye antal er nul eller mindre
            if (quantity <= 0)
            {
                // Fjerner produktet fra kurven
                _shoppingCartRepository.RemoveProduct(shoppingCart.Id, productId);

                // Sender kunden tilbage til kurven
                return RedirectToPage("/Cart");
            }

            // Finder produktet i databasen
            Product? product = _productRepository.GetById(productId);

            // Kontrollerer om produktet findes
            if (product == null)
            {
                // Sender kunden tilbage hvis produktet ikke findes
                return RedirectToPage("/Cart");
            }

            // Kontrollerer om kunden vil købe flere end der er på lager
            if (quantity > product.StockQuantity)
            {
                // Ændrer antal så det højst svarer til lageret
                quantity = product.StockQuantity;
            }

            // Gemmer det nye antal i kundens databasekurv
            _shoppingCartRepository.UpdateQuantity(shoppingCart.Id, productId, quantity);
        }

        // Sender kunden tilbage til indkøbskurven
        return RedirectToPage("/Cart");
    }

    // Hjælpemetode som henter indkøbskurven ud fra brugertypen
    private void LoadCart()
    {
        // Henter brugeren som er logget ind
        User? user = _sessionService.GetCurrentUser();

        // Hvis ingen er logget ind bruges gæstekurven
        if (user == null)
        {
            // Markerer at kurven tilhører en gæst
            IsGuest = true;

            // Henter gæstens produkter fra session
            List<ShoppingCartProduct> guestCart = _sessionService.GetGuestCart();

            // Går igennem alle produkterne i gæstekurven
            foreach (ShoppingCartProduct cartProduct in guestCart)
            {
                // Finder produktets oplysninger i databasen
                Product? product = _productRepository.GetById(cartProduct.ProductId);

                // Kontrollerer om produktet stadig findes
                if (product != null)
                {
                    // Tilføjer produktet til listen som skal vises
                    Products.Add(product);
                }
            }

            // Stopper metoden fordi gæstekurven er hentet
            return;
        }

        // Kontrollerer om brugeren er en Admin
        if (user is Admin)
        {
            // Stopper fordi admin ikke bruger indkøbskurven
            return;
        }

        // Kontrollerer om brugeren er en Customer
        if (user is not Customer customer)
        {
            // Stopper hvis brugeren ikke er en kunde
            return;
        }

        // Markerer at kurven tilhører en kunde
        IsGuest = false;

        // Finder kundens indkøbskurv i databasen
        ShoppingCart = _shoppingCartRepository.GetByCustomerId(customer.Id);

        // Kontrollerer om kunden har en kurv
        if (ShoppingCart == null)
        {
            // Stopper hvis kurven ikke findes
            return;
        }

        // Henter alle produkterne fra kundens databasekurv
        Products = _shoppingCartRepository.GetProducts(ShoppingCart.Id);
    }

    // Hjælpemetode som finder antallet af et bestemt produkt
    public int GetQuantity(int productId)
    {
        // Kontrollerer om det er en gæstekurv
        if (IsGuest)
        {
            // Henter gæstens kurv fra session
            List<ShoppingCartProduct> guestCart = _sessionService.GetGuestCart();

            // Finder det produkt som har det rigtige id
            ShoppingCartProduct? cartProduct = guestCart.FirstOrDefault(
                item => item.ProductId == productId);

            // Kontrollerer om produktet findes i kurven
            if (cartProduct == null)
            {
                // Returnerer nul hvis produktet ikke findes
                return 0;
            }

            // Returnerer antallet af produktet i gæstekurven
            return cartProduct.Quantity;
        }

        // Kontrollerer om kundens databasekurv findes
        if (ShoppingCart == null)
        {
            // Returnerer nul hvis kurven ikke findes
            return 0;
        }

        // Henter antallet af produktet fra databasen
        return _shoppingCartRepository.GetProductQuantity(
            ShoppingCart.Id, productId);
    }

    // Hjælpemetode som beregner den samlede pris for et produkt
    public decimal GetProductTotal(Product product)
    {
        // Finder hvor mange kunden har af produktet
        int quantity = GetQuantity(product.Id);

        // Ganger produktets pris med antallet
        return product.Price * quantity;
    }

    // Hjælpemetode som beregner prisen for alle produkter
    public decimal GetSubtotal()
    {
        // Starter subtotalen på nul
        decimal subtotal = 0;

        // Går igennem alle produkterne i kurven
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
        // Kontrollerer om indkøbskurven er tom
        if (Products.Count == 0)
        {
            // Returnerer nul hvis der ikke er nogen produkter
            return 0;
        }

        // Lægger fragtprisen til subtotalen
        return GetSubtotal() + ShippingPrice;
    }
}
