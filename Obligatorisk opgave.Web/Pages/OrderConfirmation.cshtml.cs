using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Obligatoris.opgave.Domain.Interfaces;
using Obligatoris.opgave.Domain.Models;
using Obligatorisk_opgave.Services;

namespace Obligatorisk_opgave.Pages;

public class OrderConfirmation : PageModel
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly SessionService _sessionService;
    private readonly AuthorizationService _authorizationService;


    public OrderConfirmation(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        SessionService sessionService,
        AuthorizationService authorizationService)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _sessionService = sessionService;
        _authorizationService = authorizationService;
    }


    public Order? Order { get; set; }


    public List<OrderProduct> OrderProducts { get; set; }
        = new List<OrderProduct>();


    public Dictionary<int, Product> Products { get; set; }
        = new Dictionary<int, Product>();


    public bool IsHistory { get; set; }


    public IActionResult OnGet(
        int id,
        bool history = false)
    {
        User? user =
            _sessionService.GetCurrentUser();


        if (user == null)
        {
            return RedirectToPage("/Login");
        }


        Order? order =
            _orderRepository.GetById(
                id);


        if (order == null)
        {
            return NotFound();
        }


        bool canViewOrder =
            _authorizationService.CanViewOrder(
                user,
                order);


        if (!canViewOrder)
        {
            return Forbid();
        }


        Order =
            order;


        IsHistory =
            history;


        OrderProducts =
            _orderRepository.GetOrderProducts(
                order.Id);


        foreach (OrderProduct orderProduct in OrderProducts)
        {
            Product? product =
                _productRepository.GetById(
                    orderProduct.ProductId);


            if (product != null)
            {
                Products[product.Id] =
                    product;
            }
        }


        return Page();
    }
}