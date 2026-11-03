using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Obligatoris.opgave.Domain.Interfaces;
using Obligatoris.opgave.Domain.Models;
using Obligatorisk_opgave.Services;

namespace Obligatorisk_opgave.Pages;

public class Account : PageModel
{
    private readonly IUserRepository _userRepository;
    private readonly SessionService _sessionService;
    private readonly AuthorizationService _authorizationService;


    public Account(
        IUserRepository userRepository,
        SessionService sessionService,
        AuthorizationService authorizationService)
    {
        _userRepository = userRepository;
        _sessionService = sessionService;
        _authorizationService = authorizationService;
    }


    [BindProperty]
    public string Name { get; set; } = "";


    [BindProperty]
    public string Surname { get; set; } = "";


    [BindProperty]
    public string Email { get; set; } = "";


    [BindProperty]
    public string Address { get; set; } = "";


    public IActionResult OnGet()
    {
        User? user =
            _sessionService.GetCurrentUser();


        if (user is not Customer customer)
        {
            return RedirectToPage("/Login");
        }


        if (!_authorizationService.OwnsAccount(
                customer,
                customer.Id))
        {
            return Forbid();
        }


        Name =
            customer.Name;

        Surname =
            customer.Surname;

        Email =
            customer.Email;

        Address =
            customer.Address;


        return Page();
    }


    public IActionResult OnPost()
    {
        User? user =
            _sessionService.GetCurrentUser();


        if (user is not Customer customer)
        {
            return RedirectToPage("/Login");
        }


        if (!_authorizationService.OwnsAccount(
                customer,
                customer.Id))
        {
            return Forbid();
        }


        Name =
            Name.Trim();

        Surname =
            Surname.Trim();

        Email =
            Email
                .Trim()
                .ToLowerInvariant();

        Address =
            Address.Trim();


        if (string.IsNullOrWhiteSpace(Name) ||
            string.IsNullOrWhiteSpace(Surname) ||
            string.IsNullOrWhiteSpace(Email) ||
            string.IsNullOrWhiteSpace(Address))
        {
            ModelState.AddModelError(
                "",
                "Please fill out all fields.");

            return Page();
        }


        // Hvis email ændres, må den nye email ikke
        // allerede tilhøre en anden bruger.
        if (!string.Equals(
                customer.Email,
                Email,
                StringComparison.OrdinalIgnoreCase) &&
            _userRepository.EmailExists(Email))
        {
            ModelState.AddModelError(
                "",
                "An account with this email already exists.");

            return Page();
        }


        customer.Name =
            Name;

        customer.Surname =
            Surname;

        customer.Email =
            Email;

        customer.Address =
            Address;


        _userRepository.Update(
            customer);


        return RedirectToPage(
            "/Account");
    }
}