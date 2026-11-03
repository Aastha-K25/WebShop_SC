using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Obligatoris.opgave.Domain.Interfaces;
using Obligatoris.opgave.Domain.Models;
using Obligatorisk_opgave.Services;

namespace Obligatorisk_opgave.Pages;

public class AdminUsers : PageModel
{
    private readonly IUserRepository _userRepository;
    private readonly SessionService _sessionService;
    private readonly AuthorizationService _authorizationService;


    public AdminUsers(
        IUserRepository userRepository,
        SessionService sessionService,
        AuthorizationService authorizationService)
    {
        _userRepository = userRepository;
        _sessionService = sessionService;
        _authorizationService = authorizationService;
    }


    // Kun Customer-konti vises på denne side
    public List<Customer> Customers { get; set; }
        = new List<Customer>();


    // Kører når siden åbnes
    public IActionResult OnGet()
    {
        User? user =
            _sessionService.GetCurrentUser();


        // Kun Admin må administrere brugere
        if (!_authorizationService.CanManageUsers(user))
        {
            return Forbid();
        }


        List<User> users =
            _userRepository.GetAll();


        // Admin administrerer kun Customer-konti her
        Customers =
            users
                .OfType<Customer>()
                .ToList();


        return Page();
    }


    // Deaktiverer en Customer
    public IActionResult OnPostDeactivate(
        int id)
    {
        User? user =
            _sessionService.GetCurrentUser();


        if (!_authorizationService.CanManageUsers(user))
        {
            return Forbid();
        }


        User? targetUser =
            _userRepository.GetById(
                id);


        // Denne side må kun administrere Customer-konti
        if (targetUser is not Customer)
        {
            return NotFound();
        }


        _userRepository.Deactivate(
            id);


        return RedirectToPage(
            "/AdminUsers");
    }


    // Aktiverer en Customer igen
    public IActionResult OnPostActivate(
        int id)
    {
        User? user =
            _sessionService.GetCurrentUser();


        if (!_authorizationService.CanManageUsers(user))
        {
            return Forbid();
        }


        User? targetUser =
            _userRepository.GetById(
                id);


        if (targetUser is not Customer customer)
        {
            return NotFound();
        }


        customer.IsActive =
            true;


        _userRepository.Update(
            customer);


        return RedirectToPage(
            "/AdminUsers");
    }
}