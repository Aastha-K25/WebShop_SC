using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Obligatoris.opgave.Domain.Interfaces;
using Obligatoris.opgave.Domain.Models;
using Obligatorisk_opgave.Services;

namespace Obligatorisk_opgave.Pages;

public class Checkout : PageModel
{
    private readonly IShoppingCartRepository _shoppingCartRepository;
    private readonly IProductRepository _productRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly SessionService _sessionService;
    private readonly AuthorizationService _authorizationService;


    // Henter repositories og services
    public Checkout(
        IShoppingCartRepository shoppingCartRepository,
        IProductRepository productRepository,
        IOrderRepository orderRepository,
        SessionService sessionService,
        AuthorizationService authorizationService)
    {
        _shoppingCartRepository = shoppingCartRepository;
        _productRepository = productRepository;
        _orderRepository = orderRepository;
        _sessionService = sessionService;
        _authorizationService = authorizationService;
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
        // Finder den bruger der er logget ind
        User? user =
            _sessionService.GetCurrentUser();


        // Kun en logget ind Customer må gå til checkout
        if (user is not Customer customer)
        {
            return RedirectToPage("/Login");
        }


        // Kontrollerer adgang ud fra ACM
        if (!_authorizationService.CanCheckout(user))
        {
            return Forbid();
        }


        Customer =
            customer;


        int customerId =
            customer.Id;


        // Finder kundens kurv
        ShoppingCart =
            _shoppingCartRepository.GetByCustomerId(
                customerId);


        // Sender tilbage hvis kurven ikke findes
        if (ShoppingCart == null)
        {
            return RedirectToPage("/Cart");
        }


        // Kontrollerer at kurven faktisk tilhører kunden
        if (!_authorizationService.OwnsCart(
                customer,
                ShoppingCart))
        {
            return Forbid();
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
        // Finder den bruger der er logget ind
        User? user =
            _sessionService.GetCurrentUser();


        // Kun en logget ind Customer må oprette en ordre
        if (user is not Customer customer)
        {
            return RedirectToPage("/Login");
        }


        // Kontrollerer adgang ud fra ACM
        if (!_authorizationService.CanCheckout(user))
        {
            return Forbid();
        }


        int customerId =
            customer.Id;


        // Finder kundens kurv
        ShoppingCart? shoppingCart =
            _shoppingCartRepository.GetByCustomerId(
                customerId);


        if (shoppingCart == null)
        {
            return RedirectToPage("/Cart");
        }


        // Kontrollerer at kurven faktisk tilhører kunden
        if (!_authorizationService.OwnsCart(
                customer,
                shoppingCart))
        {
            return Forbid();
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


            // Stopper hvis antal er ugyldigt
            if (quantity <= 0)
            {
                return RedirectToPage("/Cart");
            }


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
            _orderRepository.Add(
                order);


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


            _productRepository.Update(
                product);
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


    // Beregner den samlede pris
    public decimal GetTotalPrice()
    {
        return GetSubtotal()
            + ShippingPrice;
    }
}
