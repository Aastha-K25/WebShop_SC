using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Obligatoris.opgave.Domain.Interfaces;
using Obligatoris.opgave.Domain.Models;

namespace Obligatorisk_opgave.Pages;

public class Register : PageModel
{
    private readonly IUserRepository _userRepository;


    // Henter user repository
    public Register(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }


    // Kører når siden åbnes
    public void OnGet()
    {
    }


    // Kører når formularen sendes
    public IActionResult OnPost()
    {
        // Henter data fra formularen
        string name =
            Request.Form["name"].ToString();

        string email =
            Request.Form["email"].ToString();

        string address =
            Request.Form["address"].ToString();

        string password =
            Request.Form["password"].ToString();

        string confirmPassword =
            Request.Form["confirmPassword"].ToString();


        // Tjekker at alle felter er udfyldt
        if (string.IsNullOrWhiteSpace(name) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(address) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(confirmPassword))
        {
            TempData["ErrorMessage"] =
                "Please fill out all fields.";

            return Page();
        }


        // Tjekker at passwords er ens
        if (password != confirmPassword)
        {
            TempData["ErrorMessage"] =
                "Passwords do not match.";

            return Page();
        }


        // Tjekker om email allerede findes
        if (_userRepository.EmailExists(email))
        {
            TempData["ErrorMessage"] =
                "An account with this email already exists.";

            return Page();
        }


        // Opretter en kunde
        Customer customer = new Customer
        {
            Name = name,
            Email = email,
            Address = address,

            // Password hashing mangler stadig
            PasswordHash = password,

            IsActive = true
        };


        // Gemmer kunden i databasen
        _userRepository.Add(customer);


        // Sender videre til login
        return RedirectToPage("/Login");
    }
}