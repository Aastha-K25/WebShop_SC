
using Microsoft.AspNetCore.Mvc; // Bruges til at håndtere sider og handlinger
using Microsoft.AspNetCore.Mvc.RazorPages; // Bruges til Razor Pages

namespace Obligatorisk_opgave.Pages;

// PageModel som håndterer siden med privatlivspolitik
public class PrivacyModel : PageModel
{
    // Bruges til at logge information fra denne klasse
    private readonly ILogger<PrivacyModel> _logger;

    // Constructor som modtager vores logger
    public PrivacyModel(ILogger<PrivacyModel> logger)
    {
        // Gemmer loggeren så vi kan bruge den i klassen
        _logger = logger;
    }

    // OnGet kører når brugeren åbner Privacy siden
    public void OnGet()
    {
        // Metoden er tom fordi siden ikke skal hente eller behandle data
    }
}
