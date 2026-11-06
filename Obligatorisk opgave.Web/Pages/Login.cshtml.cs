
using Microsoft.AspNetCore.Mvc; // Bruges til at håndtere sider og handlinger
using Microsoft.AspNetCore.Mvc.RazorPages; // Bruges til Razor Pages
using Obligatoris.opgave.Domain.Interfaces; // Giver adgang til vores interfaces
using Obligatoris.opgave.Domain.Models; // Giver adgang til vores modeller
using Obligatorisk_opgave.Services; // Giver adgang til vores services

namespace Obligatorisk_opgave.Pages;

// PageModel som håndterer login og overførsel af gæstekurven
public class Login : PageModel
{
    // Bruges til at kontrollere login og password
    private readonly AuthenticationService _authenticationService;

    // Bruges til at håndtere session og gæstekurven
    private readonly SessionService _sessionService;

    // Bruges til at hente og ændre kundens indkøbskurv i databasen
    private readonly IShoppingCartRepository _shoppingCartRepository;

    // Bruges til at hente produktoplysninger fra databasen
    private readonly IProductRepository _productRepository;

    // Constructor som modtager vores services og repositories
    public Login(
        AuthenticationService authenticationService,
        SessionService sessionService,
        IShoppingCartRepository shoppingCartRepository,
        IProductRepository productRepository)
    {
        // Gemmer vores authentication service
        _authenticationService = authenticationService;

        // Gemmer vores session service
        _sessionService = sessionService;

        // Gemmer vores shopping cart repository
        _shoppingCartRepository = shoppingCartRepository;

        // Gemmer vores product repository
        _productRepository = productRepository;
    }

    // OnGet kører når brugeren åbner login siden
    public IActionResult OnGet()
    {
        // Tjekker om der allerede er en bruger logget ind
        // Vi tillader kun én bruger ad gangen i samme session
        if (_authenticationService.IsAuthenticated())
        {
            // Sender brugeren til forsiden hvis de allerede er logget ind
            return RedirectToPage("/Index");
        }

        // Viser login siden hvis ingen er logget ind
        return Page();
    }

    // OnPost kører når brugeren sender loginformularen
    public IActionResult OnPost()
    {
        // Henter email fra formularen
        // Fjerner mellemrum og laver teksten om til små bogstaver
        string email = Request.Form["email"].ToString().Trim().ToLowerInvariant();

        // Henter password fra formularen uden at ændre det
        string password = Request.Form["password"].ToString();

        // Kontrollerer om email eller password mangler
        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            // Gemmer en fejlbesked hvis et felt er tomt
            TempData["ErrorMessage"] = "Please enter email and password.";

            // Viser login siden igen
            return Page();
        }

        // Gemmer gæstens indkøbskurv før login
        // Så vi stadig har produkterne når brugeren logger ind
        List<ShoppingCartProduct> guestCart = _sessionService.GetGuestCart();

        // Sender email og password til AuthenticationService
        // Servicen kontrollerer om login er gyldigt
        string? errorMessage = _authenticationService.Login(email, password);

        // Kontrollerer om login gav en fejl
        if (errorMessage != null)
        {
            // Gemmer fejlbeskeden fra AuthenticationService
            TempData["ErrorMessage"] = errorMessage;

            // Viser login siden igen
            return Page();
        }

        // Henter brugeren som nu er logget ind
        User? currentUser = _sessionService.GetCurrentUser();

        // Kontrollerer om brugeren er en Customer
        // Tjekker også om gæstekurven indeholder produkter
        if (currentUser is Customer customer &&
            guestCart.Count > 0)
        {
            // Flytter produkterne fra gæstekurven til kundens databasekurv
            MoveGuestCartToCustomer(customer, guestCart);

            // Tømmer gæstekurven efter overførslen
            _sessionService.ClearGuestCart();

            // Sender kunden til indkøbskurven
            return RedirectToPage("/Cart");
        }

        // Sender brugeren til forsiden hvis der ikke skal flyttes en kurv
        return RedirectToPage("/Index");
    }

    // Hjælpemetode som flytter gæstens produkter til kundens indkøbskurv
    private void MoveGuestCartToCustomer(
        Customer customer,
        List<ShoppingCartProduct> guestCart)
    {
        // Finder kundens eksisterende indkøbskurv i databasen
        ShoppingCart? shoppingCart = _shoppingCartRepository.GetByCustomerId(customer.Id);

        // Kontrollerer om kunden allerede har en kurv
        if (shoppingCart == null)
        {
            // Opretter en ny indkøbskurv til kunden
            int shoppingCartId = _shoppingCartRepository.Create(customer.Id);

            // Opretter et ShoppingCart objekt med det nye id
            shoppingCart = new ShoppingCart
            {
                // Gemmer id på den nye indkøbskurv
                Id = shoppingCartId,

                // Gemmer id på kunden som ejer kurven
                CustomerId = customer.Id
            };
        }

        // Går igennem alle produkterne i gæstekurven
        foreach (ShoppingCartProduct guestProduct in guestCart)
        {
            // Finder produktet i databasen
            Product? product = _productRepository.GetById(guestProduct.ProductId);

            // Kontrollerer om produktet stadig findes
            if (product == null)
            {
                // Springer produktet over hvis det ikke findes
                continue;
            }

            // Kontrollerer om produktet er udsolgt
            if (product.StockQuantity <= 0)
            {
                // Springer produktet over hvis lageret er tomt
                continue;
            }

            // Finder hvor mange kunden allerede har af produktet i sin kurv
            int existingQuantity = _shoppingCartRepository.GetProductQuantity(
                shoppingCart.Id, guestProduct.ProductId);

            // Lægger antal fra gæstekurven sammen med kundens eksisterende antal
            int newQuantity = existingQuantity + guestProduct.Quantity;

            // Kontrollerer om det samlede antal overstiger lageret
            if (newQuantity > product.StockQuantity)
            {
                // Begrænser antallet så det højst svarer til lageret
                newQuantity = product.StockQuantity;
            }

            // Kontrollerer om produktet allerede ligger i kundens kurv
            if (existingQuantity == 0)
            {
                // Tilføjer produktet hvis det ikke findes i kurven
                _shoppingCartRepository.AddProduct(
                    shoppingCart.Id, guestProduct.ProductId, newQuantity);
            }
            else
            {
                // Opdaterer antallet hvis produktet allerede ligger i kurven
                _shoppingCartRepository.UpdateQuantity(
                    shoppingCart.Id, guestProduct.ProductId, newQuantity);
            }
        }
    }
}
