using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Obligatoris.opgave.Domain.Interfaces;
using Obligatoris.opgave.Domain.Models;
using Obligatorisk_opgave.Services;

namespace Obligatorisk_opgave.Pages;

public class Cart : PageModel
{
    private readonly IShoppingCartRepository _shoppingCartRepository;
    private readonly IProductRepository _productRepository;
    private readonly SessionService _sessionService;


    public Cart(
        IShoppingCartRepository shoppingCartRepository,
        IProductRepository productRepository,
        SessionService sessionService)
    {
        _shoppingCartRepository = shoppingCartRepository;
        _productRepository = productRepository;
        _sessionService = sessionService;
    }


    // Kundens database-kurv
    public ShoppingCart? ShoppingCart { get; set; }


    // Produkterne der skal vises
    public List<Product> Products { get; set; }
        = new List<Product>();


    // Bruges til at vide om kurven tilhører en Guest
    public bool IsGuest { get; set; }


    // Fast leveringspris
    public decimal ShippingPrice { get; set; } = 49;


    // Kører når Cart siden åbnes
    public void OnGet()
    {
        LoadCart();
    }


    // Kører når en formular på Cart siden sendes
    public IActionResult OnPost(
        string action,
        int productId,
        int quantity)
    {
        User? user =
            _sessionService.GetCurrentUser();


        // -------------------------
        // GUEST CART
        // -------------------------
        if (user == null)
        {
            // Fjerner et produkt
            if (action == "remove")
            {
                _sessionService.RemoveFromGuestCart(
                    productId);

                return RedirectToPage("/Cart");
            }


            // Tømmer hele kurven
            if (action == "clear")
            {
                _sessionService.ClearGuestCart();

                return RedirectToPage("/Cart");
            }


            // Opdaterer antal
            if (action == "update")
            {
                // Fjerner produktet hvis antal er 0
                if (quantity <= 0)
                {
                    _sessionService.RemoveFromGuestCart(
                        productId);

                    return RedirectToPage("/Cart");
                }


                Product? product =
                    _productRepository.GetById(
                        productId);


                if (product == null)
                {
                    return RedirectToPage("/Cart");
                }


                // Antal må ikke overstige lageret
                if (quantity > product.StockQuantity)
                {
                    quantity =
                        product.StockQuantity;
                }


                _sessionService.UpdateGuestCartQuantity(
                    productId,
                    quantity);
            }


            return RedirectToPage("/Cart");
        }


        // -------------------------
        // ADMIN
        // -------------------------
        // Admin må ikke bruge customer cart
        if (user is Admin)
        {
            return RedirectToPage("/Products");
        }


        // -------------------------
        // CUSTOMER CART
        // -------------------------
        if (user is not Customer customer)
        {
            return RedirectToPage("/Products");
        }


        int customerId =
            customer.Id;


        ShoppingCart? shoppingCart =
            _shoppingCartRepository.GetByCustomerId(
                customerId);


        if (shoppingCart == null)
        {
            return RedirectToPage("/Cart");
        }


        // Fjerner et produkt
        if (action == "remove")
        {
            _shoppingCartRepository.RemoveProduct(
                shoppingCart.Id,
                productId);


            return RedirectToPage("/Cart");
        }


        // Tømmer hele kurven
        if (action == "clear")
        {
            _shoppingCartRepository.Clear(
                shoppingCart.Id);


            return RedirectToPage("/Cart");
        }


        // Opdaterer antal
        if (action == "update")
        {
            if (quantity <= 0)
            {
                _shoppingCartRepository.RemoveProduct(
                    shoppingCart.Id,
                    productId);


                return RedirectToPage("/Cart");
            }


            Product? product =
                _productRepository.GetById(
                    productId);


            if (product == null)
            {
                return RedirectToPage("/Cart");
            }


            if (quantity > product.StockQuantity)
            {
                quantity =
                    product.StockQuantity;
            }


            _shoppingCartRepository.UpdateQuantity(
                shoppingCart.Id,
                productId,
                quantity);
        }


        return RedirectToPage("/Cart");
    }


    // Henter den rigtige kurv
    private void LoadCart()
    {
        User? user =
            _sessionService.GetCurrentUser();


        // -------------------------
        // GUEST
        // -------------------------
        if (user == null)
        {
            IsGuest = true;


            List<ShoppingCartProduct> guestCart =
                _sessionService.GetGuestCart();


            foreach (ShoppingCartProduct cartProduct in guestCart)
            {
                Product? product =
                    _productRepository.GetById(
                        cartProduct.ProductId);


                if (product != null)
                {
                    Products.Add(
                        product);
                }
            }


            return;
        }


        // -------------------------
        // ADMIN
        // -------------------------
        if (user is Admin)
        {
            return;
        }


        // -------------------------
        // CUSTOMER
        // -------------------------
        if (user is not Customer customer)
        {
            return;
        }


        IsGuest = false;


        ShoppingCart =
            _shoppingCartRepository.GetByCustomerId(
                customer.Id);


        if (ShoppingCart == null)
        {
            return;
        }


        Products =
            _shoppingCartRepository.GetProducts(
                ShoppingCart.Id);
    }


    // Henter antal af et produkt
    public int GetQuantity(int productId)
    {
        // Guest bruger session cart
        if (IsGuest)
        {
            List<ShoppingCartProduct> guestCart =
                _sessionService.GetGuestCart();


            ShoppingCartProduct? cartProduct =
                guestCart.FirstOrDefault(
                    item => item.ProductId == productId);


            if (cartProduct == null)
            {
                return 0;
            }


            return cartProduct.Quantity;
        }


        // Customer bruger database cart
        if (ShoppingCart == null)
        {
            return 0;
        }


        return _shoppingCartRepository.GetProductQuantity(
            ShoppingCart.Id,
            productId);
    }


    // Beregner prisen for et produkt
    public decimal GetProductTotal(Product product)
    {
        int quantity =
            GetQuantity(
                product.Id);


        return product.Price * quantity;
    }


    // Beregner subtotalen
    public decimal GetSubtotal()
    {
        decimal subtotal = 0;


        foreach (Product product in Products)
        {
            decimal productTotal =
                GetProductTotal(
                    product);


            subtotal =
                subtotal + productTotal;
        }


        return subtotal;
    }


    // Beregner samlet pris
    public decimal GetTotalPrice()
    {
        if (Products.Count == 0)
        {
            return 0;
        }


        return GetSubtotal()
            + ShippingPrice;
    }
}