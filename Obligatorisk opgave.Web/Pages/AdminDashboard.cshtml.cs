using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Obligatoris.opgave.Domain.Models;
using Obligatorisk_opgave.Services;

namespace Obligatorisk_opgave.Pages;

public class AdminDashboardModel : PageModel
{
    private readonly SessionService _sessionService;
    private readonly AuthorizationService _authorizationService;


    public AdminDashboardModel(
        SessionService sessionService,
        AuthorizationService authorizationService)
    {
        _sessionService = sessionService;
        _authorizationService = authorizationService;
    }


    public IActionResult OnGet()
    {
        User? currentUser =
            _sessionService.GetCurrentUser();


        // Ingen bruger er logget ind
        if (currentUser == null)
        {
            return RedirectToPage("/AdminLogin");
        }


        // En Customer må ikke få adgang
        if (currentUser is not Admin)
        {
            return Forbid();
        }


        // Ekstra server-side kontrol via AuthorizationService
        if (!_authorizationService.CanManageProducts(currentUser))
        {
            return Forbid();
        }


        return Page();
    }
}
