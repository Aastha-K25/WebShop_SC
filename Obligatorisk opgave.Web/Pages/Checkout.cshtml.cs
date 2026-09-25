using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Obligatoris.opgave.Domain.Interfaces;
using Obligatoris.opgave.Domain.Models;

namespace Obligatorisk_opgave.Pages;

public class Checkout : PageModel
{
    private readonly IShoppingCartRepository _shoppingCartRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUserRepository _userRepository;
    private readonly IOrderRepository _orderRepository;


    // Henter repositories
    public Checkout(
        IShoppingCartRepository shoppingCartRepository,
        IProductRepository productRepository,
        IUserRepository userRepository,
        IOrderRepository orderRepository)
    {
        _shoppingCartRepository = shoppingCartRepository;
        _productRepository = productRepository;
        _userRepository = userRepository;
        _orderRepository = orderRepository;
    }


    // Kunden
    public Customer? Customer { get; set; }


    // Kundens kurv
    public ShoppingCart? ShoppingCart { get; set; }


    // Produkterne i kurven
    public List<Product> Products { get; set; }
        = new List<Product>();


    // Fast leveringspris
    public decimal ShippingPrice { get; set; } = 49;


    // Kører når Checkout siden åbnes
    public IActionResult OnGet()
    {
        // Midlertidigt customer id indtil login er lavet
        int customerId = 1;


        // Finder kunden
        User? user =
            _userRepository.GetById(customerId);


        if (user is Customer customer)
        {
            Customer = customer;
        }
        else
        {
            return RedirectToPage("/Login");
        }


        // Finder kundens kurv
        ShoppingCart =
            _shoppingCartRepository.GetByCustomerId(customerId);


        // Sender tilbage hvis kurven ikke findes
        if (ShoppingCart == null)
        {
            return RedirectToPage("/Cart");
        }


        // Henter produkterne i kurven
        Products =
            _shoppingCartRepository.GetProducts(
                ShoppingCart.Id);


        // Sender tilbage hvis kurven er tom
        if (Products.Count == 0)
        {
            return RedirectToPage("/Cart");
        }


        return Page();
    }


    // Kører når Place Order trykkes
    public IActionResult OnPost()
    {
        // Midlertidigt customer id indtil login er lavet
        int customerId = 1;


        // Finder kundens kurv
        ShoppingCart? shoppingCart =
            _shoppingCartRepository.GetByCustomerId(customerId);


        if (shoppingCart == null)
        {
            return RedirectToPage("/Cart");
        }


        // Henter produkterne i kurven
        List<Product> products =
            _shoppingCartRepository.GetProducts(
                shoppingCart.Id);


        if (products.Count == 0)
        {
            return RedirectToPage("/Cart");
        }


        decimal subtotal = 0;


        // Beregner prisen og tjekker lager
        foreach (Product product in products)
        {
            int quantity =
                _shoppingCartRepository.GetProductQuantity(
                    shoppingCart.Id,
                    product.Id);


            // Stopper hvis der ikke er nok på lager
            if (quantity > product.StockQuantity)
            {
                return RedirectToPage("/Cart");
            }


            subtotal =
                subtotal + (product.Price * quantity);
        }


        decimal totalPrice =
            subtotal + ShippingPrice;


        // Opretter ordren
        Order order = new Order
        {
            CustomerId = customerId,
            OrderDate = DateTime.UtcNow,
            TotalPrice = totalPrice,
            Status = OrderStatus.Pending
        };


        // Gemmer ordren
        int orderId =
            _orderRepository.Add(order);


        // Gemmer produkterne på ordren
        foreach (Product product in products)
        {
            int quantity =
                _shoppingCartRepository.GetProductQuantity(
                    shoppingCart.Id,
                    product.Id);


            _orderRepository.AddProductToOrder(
                orderId,
                product.Id,
                quantity,
                product.Price);


            // Trækker antal fra lageret
            product.StockQuantity =
                product.StockQuantity - quantity;


            _productRepository.Update(product);
        }


        // Tømmer kurven
        _shoppingCartRepository.Clear(
            shoppingCart.Id);


        // Sender videre til ordrebekræftelse
        return RedirectToPage(
            "/OrderConfirmation",
            new { id = orderId });
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


    // Beregner den samlede pris
    public decimal GetTotalPrice()
    {
        return GetSubtotal() + ShippingPrice;
    }
}