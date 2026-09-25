using Microsoft.AspNetCore.Mvc.RazorPages;
using Obligatoris.opgave.Domain.Interfaces;
using Obligatoris.opgave.Domain.Models;

namespace Obligatorisk_opgave.Pages;

public class OrderConfirmation : PageModel
{
    private readonly IOrderRepository _orderRepository;


    // Henter order repository
    public OrderConfirmation(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }


    // Ordren der skal vises
    public Order? Order { get; set; }


    // Kører når siden åbnes
    public void OnGet(int id)
    {
        Order =
            _orderRepository.GetById(id);
    }
}