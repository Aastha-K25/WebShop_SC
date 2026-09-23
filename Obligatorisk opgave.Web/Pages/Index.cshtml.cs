using Microsoft.AspNetCore.Mvc.RazorPages;
using Obligatoris.opgave.Domain.Interfaces;
using Obligatoris.opgave.Domain.Models;

namespace Obligatorisk_opgave.Pages;

public class IndexModel : PageModel
{
    private readonly IProductRepository _productRepository;

    // Buketterne som forsiden skal vise
    public IReadOnlyList<Bouquet> PopularProducts { get; private set; }
        = Array.Empty<Bouquet>();

    public IndexModel(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    // Kører når forsiden åbnes
    public void OnGet()
    {
        PopularProducts =
            _productRepository.GetPopularBouquets(4);
    }
}