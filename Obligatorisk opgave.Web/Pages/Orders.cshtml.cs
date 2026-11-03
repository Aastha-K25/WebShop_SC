using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Obligatoris.opgave.Domain.Interfaces;
using Obligatoris.opgave.Domain.Models;
using Obligatorisk_opgave.Services;

namespace Obligatorisk_opgave.Pages;

public class Orders : PageModel
{
    private readonly IOrderRepository _orderRepository;
    private readonly SessionService _sessionService;


    public Orders(
        IOrderRepository orderRepository,
        SessionService sessionService)
    {
        _orderRepository = orderRepository;
        _sessionService = sessionService;
    }


    public List<Order> CustomerOrders { get; set; }
        = new List<Order>();


    public IActionResult OnGet()
    {
        // Finder den bruger der er logget ind
        User? user =
            _sessionService.GetCurrentUser();


        // Kun Customer må se egne ordrer
        if (user is not Customer customer)
        {
            return RedirectToPage("/Login");
        }


        // Henter kun den aktuelle kundes ordrer
        CustomerOrders =
            _orderRepository.GetByCustomerId(
                customer.Id);


        return Page();
    }
}