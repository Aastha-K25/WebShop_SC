using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Obligatoris.opgave.Domain.Interfaces;
using Obligatoris.opgave.Domain.Models;

namespace Obligatorisk_opgave.Pages;

public class Cart : PageModel
{
    private readonly IShoppingCartRepository _shoppingCartRepository;
    private readonly IProductRepository _productRepository;


    // Henter repositories
    public Cart(
        IShoppingCartRepository shoppingCartRepository,
        IProductRepository productRepository)
    {
        _shoppingCartRepository = shoppingCartRepository;
        _productRepository = productRepository;
    }


    // Kundens kurv
    public ShoppingCart? ShoppingCart { get; set; }


    // Produkterne i kurven
    public List<Product> Products { get; set; }
        = new List<Product>();


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
        // Midlertidigt customer id indtil login er lavet
        int customerId = 1;


        // Finder kundens kurv
        ShoppingCart? shoppingCart =
            _shoppingCartRepository.GetByCustomerId(customerId);


        // Stopper hvis kunden ikke har en kurv
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
            // Fjerner produktet hvis antal er 0
            if (quantity <= 0)
            {
                _shoppingCartRepository.RemoveProduct(
                    shoppingCart.Id,
                    productId);


                return RedirectToPage("/Cart");
            }


            // Finder produktet
            Product? product =
                _productRepository.GetById(productId);


            if (product == null)
            {
                return RedirectToPage("/Cart");
            }


            // Antallet må ikke være større end lageret
            if (quantity > product.StockQuantity)
            {
                quantity = product.StockQuantity;
            }


            _shoppingCartRepository.UpdateQuantity(
                shoppingCart.Id,
                productId,
                quantity);
        }


        return RedirectToPage("/Cart");
    }


    // Henter kundens kurv og produkter
    private void LoadCart()
    {
        // Midlertidigt customer id indtil login er lavet
        int customerId = 1;


        ShoppingCart =
            _shoppingCartRepository.GetByCustomerId(customerId);


        // Stopper hvis kunden ikke har en kurv
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
            GetQuantity(product.Id);


        return product.Price * quantity;
    }


    // Beregner subtotalen
    public decimal GetSubtotal()
    {
        decimal subtotal = 0;


        foreach (Product product in Products)
        {
            decimal productTotal =
                GetProductTotal(product);


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


        return GetSubtotal() + ShippingPrice;
    }
}