using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Obligatoris.opgave.Domain.Models;


namespace Obligatorisk_opgave.Pages;

public class IndexModel : PageModel
{
    //Produkterne, som Index.cshtml skal vise.
    //IReadOnlyList bruges, fordi Razor-siden kun skal læse produkterne og ikke ændre samlingen.
    public IReadOnlyList<Product> PopularProducts { get; private set; } 
    = Array.Empty<Product>();

    // OnGet bliver automatisk kaldt, når browseren sender en GET-request til forsiden.
    
    public void OnGet()
    {
        PopularProducts = new List<Product>
        {
            new Product
            {
                Id = 1,
                Name = "Romantisk buket",
                BookGenre = "Romance",
                Price = 449m,
                Imagepath = "..",
                ImageDescription = "Røde buket med en romantisk bog",
                IsPopular = true
            },
            new Product
            {
                Id = 2,
                Name = "Krimi buket",
                BookGenre = "Krimi",
                Price = 449m,
                Imagepath = "..",
                ImageDescription = "Hvide buket med en krimi bog",
                IsPopular = true
            },
            new Product
            {
                Id = 3,
                Name = "Fantasy buket",
                BookGenre = "Fantasy",
                Price = 449m,
                Imagepath = "..",
                ImageDescription = "Lilla buket med en fantasy bog",
                IsPopular = true,
            },
            new Product
            {
                Id = 4,
                Name = "Biografi buket",
                BookGenre = "Biografi",
                Price = 449m,
                Imagepath = "..",
                ImageDescription = "Orange buket med en biografi bog",
                IsPopular = true,
            }
        };
    }
}