using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Obligatoris.opgave.Domain.Interfaces;
using Obligatoris.opgave.Domain.Models;

namespace Obligatorisk_opgave.Pages;

public class ProductDetails : PageModel
{
    private readonly IProductRepository _productRepository;

    public Bouquet Bouquet { get; set; } = null!;

    public List<Bouquet> Sizes { get; set; } = new List<Bouquet>();


    public ProductDetails(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }


    public IActionResult OnGet(int id)
    {
        Product? product =
            _productRepository.GetById(id);

        if (product is not Bouquet bouquet)
        {
            return NotFound();
        }

        Bouquet = bouquet;


        // Henter de andre størrelser inden for samme genre.
        Sizes =
            _productRepository.GetBouquetsByGenre(
                bouquet.BookGenre);


        return Page();
    }
}