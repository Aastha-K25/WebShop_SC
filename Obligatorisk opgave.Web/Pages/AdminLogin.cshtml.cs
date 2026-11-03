using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Obligatoris.opgave.Domain.Models;
using Obligatorisk_opgave.Services;

namespace Obligatorisk_opgave.Pages;

public class AdminLoginModel : PageModel
{
    private readonly AuthenticationService _authenticationService;


    public AdminLoginModel(
        AuthenticationService authenticationService)
    {
        _authenticationService =
            authenticationService;
    }


    [BindProperty]
    public string Email { get; set; } = "";


    [BindProperty]
    public string Password { get; set; } = "";


    public string? ErrorMessage { get; set; }


    public IActionResult OnGet()
    {
        User? currentUser =
            _authenticationService.GetCurrentUser();


        // Hvis Admin allerede er logget ind,
        // sendes brugeren direkte til dashboardet
        if (currentUser is Admin)
        {
            return Redirect("/AdminDashboard");
        }


        return Page();
    }


    public IActionResult OnPost()
    {
        // Genbruger den eksisterende login-logik
        string? loginError =
            _authenticationService.Login(
                Email,
                Password);


        if (loginError != null)
        {
            ErrorMessage =
                loginError;

            return Page();
        }


        User? currentUser =
            _authenticationService.GetCurrentUser();


        // Kun en rigtig Admin må fortsætte
        if (currentUser is not Admin)
        {
            _authenticationService.Logout();

            ErrorMessage =
                "Invalid admin email or password.";

            return Page();
        }


        return Redirect("/AdminDashboard");
    }
}
