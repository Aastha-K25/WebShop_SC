using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Obligatoris.opgave.Domain.Models;
using Obligatorisk_opgave.Services;

namespace Obligatorisk_opgave.Pages;

public class ChangePassword : PageModel
{
    private readonly AuthenticationService _authenticationService;

    public ChangePassword(
        AuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    // Viser kun siden til en kunde, der er logget ind
    public IActionResult OnGet()
    {
        User? currentUser =
            _authenticationService.GetCurrentUser();

        if (currentUser is not Customer)
        {
            return RedirectToPage("/Login");
        }

        return Page();
    }

    // Kører når kunden sender formularen
    public IActionResult OnPost()
    {
        User? currentUser =
            _authenticationService.GetCurrentUser();

        // Kontrollerer igen på serveren, at brugeren er Customer
        if (currentUser is not Customer)
        {
            return RedirectToPage("/Login");
        }

        string currentPassword =
            Request.Form["currentPassword"].ToString();

        string newPassword =
            Request.Form["newPassword"].ToString();

        string confirmPassword =
            Request.Form["confirmPassword"].ToString();

        // Alle felter skal være udfyldt
        if (string.IsNullOrWhiteSpace(currentPassword) ||
            string.IsNullOrWhiteSpace(newPassword) ||
            string.IsNullOrWhiteSpace(confirmPassword))
        {
            TempData["ErrorMessage"] =
                "Please fill out all fields.";

            return Page();
        }

        // Det nye password skal skrives ens begge gange
        if (newPassword != confirmPassword)
        {
            TempData["ErrorMessage"] =
                "The new passwords do not match.";

            return Page();
        }

        string? errorMessage =
            _authenticationService.ChangePassword(
                currentPassword,
                newPassword);

        if (errorMessage != null)
        {
            TempData["ErrorMessage"] = errorMessage;
            return Page();
        }

        // Sessionen afsluttes efter ændringen,
        // så kunden skal logge ind med det nye password
        _authenticationService.Logout();

        TempData["SuccessMessage"] =
            "Your password has been changed. Please log in again.";

        return RedirectToPage("/Login");
    }
}