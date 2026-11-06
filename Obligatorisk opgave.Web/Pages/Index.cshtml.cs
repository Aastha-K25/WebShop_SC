
using Microsoft.AspNetCore.Mvc.RazorPages; // Bruges til Razor Pages
using Obligatoris.opgave.Domain.Interfaces; // Giver adgang til vores interfaces
using Obligatoris.opgave.Domain.Models; // Giver adgang til vores modeller

namespace Obligatorisk_opgave.Pages;

// PageModel som håndterer forsiden af webshoppen
public class IndexModel : PageModel
{
    // Bruges til at hente produkter fra databasen
    private readonly IProductRepository _productRepository;

    // Gemmer de populære buketter som skal vises på forsiden
    // IReadOnlyList betyder at listen kun kan læses gennem denne property
    public IReadOnlyList<Bouquet> PopularProducts { get; private set; } = Array.Empty<Bouquet>();

    // Constructor som modtager vores product repository
    public IndexModel(IProductRepository productRepository)
    {
        // Gemmer vores repository så vi kan bruge det i klassen
        _productRepository = productRepository;
    }

    // OnGet kører når forsiden åbnes
    public void OnGet()
    {
        // Henter fire populære buketter fra databasen
        // Gemmer dem i PopularProducts så de kan vises på forsiden
        PopularProducts = _productRepository.GetPopularBouquets(4);
    }
}
