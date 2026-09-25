using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Obligatoris.opgave.Domain.Interfaces;
using Obligatoris.opgave.Domain.Models;

namespace Obligatorisk_opgave.Pages;

public class Login : PageModel
{
    private readonly IUserRepository _userRepository;


    // Henter user repository
    public Login(IUserRepository userRepository)
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
        string email =
            Request.Form["email"].ToString();

        string password =
            Request.Form["password"].ToString();


        // Tjekker at felterne er udfyldt
        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            TempData["ErrorMessage"] =
                "Please enter email and password.";

            return Page();
        }


        // Finder brugeren ud fra email
        User? user =
            _userRepository.GetByEmail(email);


        // Tjekker om brugeren findes
        if (user == null)
        {
            TempData["ErrorMessage"] =
                "Invalid email or password.";

            return Page();
        }


        // Tjekker om brugeren er aktiv
        if (!user.IsActive)
        {
            TempData["ErrorMessage"] =
                "Invalid email or password.";

            return Page();
        }


        // Password verification mangler stadig
        TempData["ErrorMessage"] =
            "Password verification is not connected yet.";

        return Page();
    }
}