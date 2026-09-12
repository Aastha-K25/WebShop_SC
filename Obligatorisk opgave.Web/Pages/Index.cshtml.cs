using Microsoft.AspNetCore.Mvc.RazorPages;
using Obligatoris.opgave.Domain.Models;

namespace Obligatorisk_opgave.Pages;

public class IndexModel : PageModel
{
    // Buketterne som forsiden skal vise
    public IReadOnlyList<Bouquet> PopularProducts { get; private set; }
        = Array.Empty<Bouquet>();

    // Kører når forsiden åbnes
    public void OnGet()
    {
        PopularProducts = new List<Bouquet>
        {
            new Bouquet
            {
                Id = 1,
                Name = "Romance Bouquet",
                BookGenre = "Romance",
                Price = 449m,
                ImagePath = "/Images/romanceall.png",
                ImageDescription = "Romance bouquet with books and flowers",
                IsPopular = true
            },

            new Bouquet
            {
                Id = 2,
                Name = "Crime Bouquet",
                BookGenre = "Crime",
                Price = 449m,
                ImagePath = "/Images/crimeall.png",
                ImageDescription = "Crime bouquet with books and flowers",
                IsPopular = true
            },

            new Bouquet
            {
                Id = 3,
                Name = "Fantasy Bouquet",
                BookGenre = "Fantasy",
                Price = 449m,
                ImagePath = "/Images/fantasyall.png",
                ImageDescription = "Fantasy bouquet with books and flowers",
                IsPopular = true
            },

            new Bouquet
            {
                Id = 4,
                Name = "Biography Bouquet",
                BookGenre = "Biography",
                Price = 449m,
                ImagePath = "/Images/Bioall.png",
                ImageDescription = "Biography bouquet with books and flowers",
                IsPopular = true
            }
        };
    }
}