using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Obligatoris.opgave.Domain.Interfaces;
using Obligatoris.opgave.Domain.Models;
using Obligatorisk_opgave.Services;

namespace Obligatorisk_opgave.Pages;

public class AdminOrders : PageModel
{
    private readonly IOrderRepository _orderRepository;
    private readonly SessionService _sessionService;
    private readonly AuthorizationService _authorizationService;


    public AdminOrders(
        IOrderRepository orderRepository,
        SessionService sessionService,
        AuthorizationService authorizationService)
    {
        _orderRepository = orderRepository;
        _sessionService = sessionService;
        _authorizationService = authorizationService;
    }


    // Alle ordrer som Admin må se
    public List<Order> Orders { get; set; }
        = new List<Order>();


    // Kører når siden åbnes
    public IActionResult OnGet()
    {
        User? user =
            _sessionService.GetCurrentUser();


        // Kun Admin må se alle ordrer
        if (!_authorizationService.CanManageOrders(user))
        {
            return Forbid();
        }


        Orders =
            _orderRepository.GetAll();


        return Page();
    }


    // Kører når Admin ændrer ordrestatus
    public IActionResult OnPostUpdateStatus(
        int orderId,
        OrderStatus newStatus)
    {
        User? user =
            _sessionService.GetCurrentUser();


        // Kun Admin må ændre ordrestatus
        if (!_authorizationService.CanManageOrders(user))
        {
            return Forbid();
        }


        // Finder ordren
        Order? order =
            _orderRepository.GetById(
                orderId);


        if (order == null)
        {
            return NotFound();
        }


        // Kontrollerer at status-skiftet er tilladt
        if (!CanChangeOrderStatus(
                order.Status,
                newStatus))
        {
            return BadRequest();
        }


        // Gemmer den nye status
        _orderRepository.UpdateStatus(
            orderId,
            newStatus);


        return RedirectToPage(
            "/AdminOrders");
    }


    // Regler fra vores Order State Diagram
    private bool CanChangeOrderStatus(
        OrderStatus currentStatus,
        OrderStatus newStatus)
    {
        if (currentStatus == OrderStatus.Pending)
        {
            return newStatus == OrderStatus.Paid
                || newStatus == OrderStatus.Cancelled;
        }


        if (currentStatus == OrderStatus.Paid)
        {
            return newStatus == OrderStatus.Processing
                || newStatus == OrderStatus.Cancelled;
        }


        if (currentStatus == OrderStatus.Processing)
        {
            return newStatus == OrderStatus.Shipped
                || newStatus == OrderStatus.Cancelled;
        }


        if (currentStatus == OrderStatus.Shipped)
        {
            return newStatus == OrderStatus.Completed;
        }


        // Completed og Cancelled er sluttilstande
        return false;
    }
}