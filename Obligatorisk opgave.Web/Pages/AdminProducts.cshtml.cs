
using Microsoft.AspNetCore.Mvc; // Bruges til at håndtere sider og handlinger
using Microsoft.AspNetCore.Mvc.RazorPages; // Bruges til Razor Pages
using Obligatoris.opgave.Domain.Interfaces; // Giver adgang til vores interfaces
using Obligatoris.opgave.Domain.Models; // Giver adgang til vores modeller
using Obligatorisk_opgave.Services; // Giver adgang til vores services

namespace Obligatorisk_opgave.Pages;

// PageModel som håndterer administration af produkter
public class AdminProducts : PageModel
{
    // Bruges til at hente og ændre produkter i databasen
    private readonly IProductRepository _productRepository;

    // Bruges til at finde den bruger som er logget ind
    private readonly SessionService _sessionService;

    // Bruges til at kontrollere om brugeren har adgang
    private readonly AuthorizationService _authorizationService;

    // Constructor som modtager vores repository og services
    public AdminProducts(
        IProductRepository productRepository,
        SessionService sessionService,
        AuthorizationService authorizationService)
    {
        // Gemmer vores product repository
        _productRepository = productRepository;

        // Gemmer vores session service
        _sessionService = sessionService;

        // Gemmer vores authorization service
        _authorizationService = authorizationService;
    }

    // Opretter en tom liste som skal indeholde vores produkter
    public List<Product> Products { get; set; } = new List<Product>();

    // Binder Bouquet til formularen på hjemmesiden
    [BindProperty]
    public Bouquet Bouquet { get; set; } = new Bouquet
    {
        // Starter navnet som en tom tekst
        Name = "",

        // Starter blomstertypen som en tom tekst
        FlowerType = ""
    };

    // OnGet kører når admin åbner produktsiden
    public IActionResult OnGet()
    {
        // Henter brugeren som er logget ind
        User? user = _sessionService.GetCurrentUser();

        // Kontrollerer om brugeren må administrere produkter
        if (!_authorizationService.CanManageProducts(user))
        {
            // Afviser adgang hvis brugeren ikke har tilladelse
            return Forbid();
        }

        // Henter alle produkter fra databasen
        Products = _productRepository.GetAll();

        // Viser siden med produkterne
        return Page();
    }

    // OnPostCreate kører når admin opretter et nyt produkt
    public IActionResult OnPostCreate()
    {
        // Henter brugeren som er logget ind
        User? user = _sessionService.GetCurrentUser();

        // Kontrollerer om brugeren må oprette produkter
        if (!_authorizationService.CanManageProducts(user))
        {
            // Afviser adgang hvis brugeren ikke har tilladelse
            return Forbid();
        }

        // Finder antal bøger ud fra bukettens størrelse
        Bouquet.NumberOfBooks = GetNumberOfBooks(Bouquet.BouquetSize);

        // Kontrollerer om bukettens oplysninger er gyldige
        if (!IsValidBouquet(Bouquet))
        {
            // Henter produkterne igen hvis valideringen fejler
            Products = _productRepository.GetAll();

            // Viser siden igen uden at oprette produktet
            return Page();
        }

        // Gemmer den nye buket i databasen
        _productRepository.Add(Bouquet);

        // Sender admin tilbage til produktsiden
        return RedirectToPage("/AdminProducts");
    }

    // OnPostUpdate kører når admin opdaterer et produkt
    public IActionResult OnPostUpdate()
    {
        // Henter brugeren som er logget ind
        User? user = _sessionService.GetCurrentUser();

        // Kontrollerer om brugeren må ændre produkter
        if (!_authorizationService.CanManageProducts(user))
        {
            // Afviser adgang hvis brugeren ikke har tilladelse
            return Forbid();
        }

        // Finder det eksisterende produkt i databasen
        Product? existingProduct = _productRepository.GetById(Bouquet.Id);

        // Kontrollerer om produktet findes
        if (existingProduct == null)
        {
            // Returnerer NotFound hvis produktet ikke findes
            return NotFound();
        }

        // Finder antal bøger ud fra bukettens størrelse
        Bouquet.NumberOfBooks = GetNumberOfBooks(Bouquet.BouquetSize);

        // Kontrollerer om de nye oplysninger er gyldige
        if (!IsValidBouquet(Bouquet))
        {
            // Henter produkterne igen hvis valideringen fejler
            Products = _productRepository.GetAll();

            // Viser siden igen uden at gemme ændringerne
            return Page();
        }

        // Opdaterer buketten i databasen
        _productRepository.Update(Bouquet);

        // Sender admin tilbage til produktsiden
        return RedirectToPage("/AdminProducts");
    }

    // OnPostDelete kører når admin sletter et produkt
    public IActionResult OnPostDelete(int id)
    {
        // Henter brugeren som er logget ind
        User? user = _sessionService.GetCurrentUser();

        // Kontrollerer om brugeren må slette produkter
        if (!_authorizationService.CanManageProducts(user))
        {
            // Afviser adgang hvis brugeren ikke har tilladelse
            return Forbid();
        }

        // Finder produktet som skal slettes
        Product? product = _productRepository.GetById(id);

        // Kontrollerer om produktet findes
        if (product == null)
        {
            // Returnerer NotFound hvis produktet ikke findes
            return NotFound();
        }

        // Sletter produktet fra databasen
        _productRepository.Delete(id);

        // Sender admin tilbage til produktsiden
        return RedirectToPage("/AdminProducts");
    }

    // Hjælpemetode som bestemmer antal bøger ud fra bukettens størrelse
    private int GetNumberOfBooks(BouquetSize bouquetSize)
    {
        // Tjekker om buketten er Small
        if (bouquetSize == BouquetSize.Small)
        {
            // En lille buket indeholder 2 bøger
            return 2;
        }

        // Tjekker om buketten er Medium
        if (bouquetSize == BouquetSize.Medium)
        {
            // En mellem buket indeholder 3 bøger
            return 3;
        }

        // Andre størrelser får 5 bøger
        return 5;
    }

    // Hjælpemetode som kontrollerer om bukettens oplysninger er gyldige
    private bool IsValidBouquet(Bouquet bouquet)
    {
        // Kontrollerer om bukettens navn mangler
        if (string.IsNullOrWhiteSpace(bouquet.Name))
        {
            // Returnerer false hvis navnet er tomt
            return false;
        }

        // Kontrollerer om blomstertypen mangler
        if (string.IsNullOrWhiteSpace(bouquet.FlowerType))
        {
            // Returnerer false hvis blomstertypen er tom
            return false;
        }

        // Kontrollerer om prisen er negativ
        if (bouquet.Price < 0)
        {
            // Afviser produktet hvis prisen er under nul
            return false;
        }

        // Kontrollerer om lagerantallet er negativt
        if (bouquet.StockQuantity < 0)
        {
            // Afviser produktet hvis lagerantallet er under nul
            return false;
        }

        // Kontrollerer om boggenren findes i vores BookGenre enum
        if (!Enum.IsDefined(typeof(BookGenre), bouquet.BookGenre))
        {
            // Afviser produktet hvis genren er ugyldig
            return false;
        }

        // Kontrollerer om størrelsen findes i vores BouquetSize enum
        if (!Enum.IsDefined(typeof(BouquetSize), bouquet.BouquetSize))
        {
            // Afviser produktet hvis størrelsen er ugyldig
            return false;
        }

        // Kontrollerer om antallet af bøger er større end nul
        if (bouquet.NumberOfBooks <= 0)
        {
            // Afviser produktet hvis antal bøger er ugyldigt
            return false;
        }

        // Returnerer true hvis alle oplysninger er gyldige
        return true;
    }
}
