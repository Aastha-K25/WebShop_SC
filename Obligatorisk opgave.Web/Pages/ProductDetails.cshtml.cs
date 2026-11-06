
using Microsoft.AspNetCore.Mvc; // Bruges til at håndtere sider og handlinger
using Microsoft.AspNetCore.Mvc.RazorPages; // Bruges til Razor Pages
using Obligatoris.opgave.Domain.Interfaces; // Giver adgang til vores interfaces
using Obligatoris.opgave.Domain.Models; // Giver adgang til vores modeller

namespace Obligatorisk_opgave.Pages;

// PageModel som håndterer siden med produktets detaljer
public class ProductDetails : PageModel
{
    // Bruges til at hente produkter fra databasen
    private readonly IProductRepository _productRepository;

    // Gemmer buketten som kunden har valgt
    // null! fortæller C# at værdien bliver sat senere
    public Bouquet Bouquet { get; set; } = null!;

    // Opretter en tom liste til buketter med samme genre
    public List<Bouquet> Sizes { get; set; } = new List<Bouquet>();

    // Constructor som modtager vores product repository
    public ProductDetails(IProductRepository productRepository)
    {
        // Gemmer vores repository så vi kan bruge det i klassen
        _productRepository = productRepository;
    }

    // OnGet kører når kunden åbner en bestemt produktside
    // id fortæller hvilket produkt der skal hentes
    public IActionResult OnGet(int id)
    {
        // Finder produktet i databasen ud fra dets id
        Product? product = _productRepository.GetById(id);

        // Kontrollerer om produktet findes og er en Bouquet
        if (product is not Bouquet bouquet)
        {
            // Returnerer NotFound hvis produktet ikke er en buket
            return NotFound();
        }

        // Gemmer den valgte buket så den kan vises på siden
        Bouquet = bouquet;

        // Henter alle buketter med samme boggenre
        // Bruges til at vise de andre størrelser i samme genre
        Sizes = _productRepository.GetBouquetsByGenre(bouquet.BookGenre);

        // Viser produktsiden med buketten og de tilhørende varianter
        return Page();
    }
}
