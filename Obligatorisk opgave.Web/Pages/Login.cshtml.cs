using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Obligatoris.opgave.Domain.Interfaces;
using Obligatoris.opgave.Domain.Models;
using Obligatorisk_opgave.Services;

namespace Obligatorisk_opgave.Pages;

public class Login : PageModel
{
    private readonly AuthenticationService _authenticationService;
    private readonly SessionService _sessionService;
    private readonly IShoppingCartRepository _shoppingCartRepository;
    private readonly IProductRepository _productRepository;


    public Login(
        AuthenticationService authenticationService,
        SessionService sessionService,
        IShoppingCartRepository shoppingCartRepository,
        IProductRepository productRepository)
    {
        _authenticationService = authenticationService;
        _sessionService = sessionService;
        _shoppingCartRepository = shoppingCartRepository;
        _productRepository = productRepository;
    }


    public IActionResult OnGet()
    {
        // Kun én bruger ad gangen i den aktuelle session
        if (_authenticationService.IsAuthenticated())
        {
            return RedirectToPage("/Index");
        }

        return Page();
    }


    public IActionResult OnPost()
    {
        string email =
            Request.Form["email"]
                .ToString()
                .Trim()
                .ToLowerInvariant();

        string password =
            Request.Form["password"]
                .ToString();


        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            TempData["ErrorMessage"] =
                "Please enter email and password.";

            return Page();
        }


        // Gemmer guest cart før login,
        // fordi login opretter en customer-session
        List<ShoppingCartProduct> guestCart =
            _sessionService.GetGuestCart();


        string? errorMessage =
            _authenticationService.Login(
                email,
                password);


        if (errorMessage != null)
        {
            TempData["ErrorMessage"] =
                errorMessage;

            return Page();
        }


        User? currentUser =
            _sessionService.GetCurrentUser();


        // Hvis en guest logger ind som Customer,
        // flyttes guest cart over i database-cart
        if (currentUser is Customer customer &&
            guestCart.Count > 0)
        {
            MoveGuestCartToCustomer(
                customer,
                guestCart);

            _sessionService.ClearGuestCart();

            return RedirectToPage("/Cart");
        }


        return RedirectToPage("/Index");
    }


    private void MoveGuestCartToCustomer(
        Customer customer,
        List<ShoppingCartProduct> guestCart)
    {
        ShoppingCart? shoppingCart =
            _shoppingCartRepository.GetByCustomerId(
                customer.Id);


        // Opretter database-cart hvis kunden ikke har en endnu
        if (shoppingCart == null)
        {
            int shoppingCartId =
                _shoppingCartRepository.Create(
                    customer.Id);

            shoppingCart =
                new ShoppingCart
                {
                    Id = shoppingCartId,
                    CustomerId = customer.Id
                };
        }


        foreach (ShoppingCartProduct guestProduct in guestCart)
        {
            Product? product =
                _productRepository.GetById(
                    guestProduct.ProductId);


            if (product == null)
            {
                continue;
            }


            if (product.StockQuantity <= 0)
            {
                continue;
            }


            int existingQuantity =
                _shoppingCartRepository.GetProductQuantity(
                    shoppingCart.Id,
                    guestProduct.ProductId);


            int newQuantity =
                existingQuantity +
                guestProduct.Quantity;


            // Må ikke overstige lageret
            if (newQuantity > product.StockQuantity)
            {
                newQuantity =
                    product.StockQuantity;
            }


            if (existingQuantity == 0)
            {
                _shoppingCartRepository.AddProduct(
                    shoppingCart.Id,
                    guestProduct.ProductId,
                    newQuantity);
            }
            else
            {
                _shoppingCartRepository.UpdateQuantity(
                    shoppingCart.Id,
                    guestProduct.ProductId,
                    newQuantity);
            }
        }
    }
}