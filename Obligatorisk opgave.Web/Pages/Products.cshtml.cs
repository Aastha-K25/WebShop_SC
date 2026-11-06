
using Microsoft.AspNetCore.Mvc; // Bruges til at håndtere sider og handlinger
using Microsoft.AspNetCore.Mvc.RazorPages; // Bruges til Razor Pages
using Obligatoris.opgave.Domain.Interfaces; // Giver adgang til vores interfaces
using Obligatoris.opgave.Domain.Models; // Giver adgang til vores modeller
using Obligatorisk_opgave.Services; // Giver adgang til vores services

namespace Obligatorisk_opgave.Pages;

// PageModel som håndterer produktsiden og Add to Bag
public class Products : PageModel
{
    // Bruges til at hente produkter fra databasen
    private readonly IProductRepository _productRepository;

    // Bruges til at hente og ændre kundens indkøbskurv
    private readonly IShoppingCartRepository _shoppingCartRepository;

    // Bruges til at finde brugeren og håndtere gæstekurven
    private readonly SessionService _sessionService;

    // Constructor som modtager vores repositories og session service
    public Products(
        IProductRepository productRepository,
        IShoppingCartRepository shoppingCartRepository,
        SessionService sessionService)
    {
        // Gemmer vores product repository
        _productRepository = productRepository;

        // Gemmer vores shopping cart repository
        _shoppingCartRepository = shoppingCartRepository;

        // Gemmer vores session service
        _sessionService = sessionService;
    }

    // Opretter en tom liste til buketterne som skal vises
    public List<Bouquet> Bouquets { get; set; } = new List<Bouquet>();

    // Opretter en tom liste til populære buketter
    public List<Bouquet> PopularBouquets { get; set; } = new List<Bouquet>();

    // Gemmer den genre som brugeren har valgt
    // Spørgsmålstegnet betyder at værdien må være null
    public BookGenre? SelectedGenre { get; set; }

    // Gemmer teksten som brugeren søger efter
    public string SearchTerm { get; set; } = string.Empty;

    // OnGet kører når brugeren åbner produktsiden
    public void OnGet(string? genre, string? search)
    {
        // Henter fire populære buketter fra databasen
        PopularBouquets = _productRepository.GetPopularBouquets(4);

        // Fjerner ekstra mellemrum fra søgeteksten
        // Bruger en tom tekst hvis search er null
        SearchTerm = search?.Trim() ?? string.Empty;

        // Kontrollerer om søgeteksten er længere end 200 tegn
        if (SearchTerm.Length > 200)
        {
            // Beholder kun de første 200 tegn
            SearchTerm = SearchTerm[..200];
        }

        // Kontrollerer om brugeren har skrevet noget i søgefeltet
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            // Søger efter buketter som matcher teksten
            Bouquets = _productRepository.SearchBouquets(SearchTerm);

            // Stopper metoden fordi søgeresultaterne er hentet
            return;
        }

        // Kontrollerer om brugeren har valgt en genre
        if (string.IsNullOrWhiteSpace(genre))
        {
            // Henter alle buketter hvis ingen genre er valgt
            Bouquets = _productRepository.GetAllBouquets();

            // Stopper metoden fordi alle buketter er hentet
            return;
        }

        // Opretter en variabel til den valgte genre
        BookGenre selectedGenre;

        // Forsøger at lave teksten om til en BookGenre enum
        // true betyder at store og små bogstaver ikke gør en forskel
        // out gemmer resultatet i selectedGenre
        bool genreExists = Enum.TryParse(genre, true, out selectedGenre);

        // Kontrollerer om genren kunne læses og findes i vores enum
        if (!genreExists ||
            !Enum.IsDefined(typeof(BookGenre), selectedGenre))
        {
            // Viser alle buketter hvis genren er ugyldig
            Bouquets = _productRepository.GetAllBouquets();

            // Stopper metoden
            return;
        }

        // Gemmer den genre som brugeren har valgt
        SelectedGenre = selectedGenre;

        // Henter kun buketter som hører til den valgte genre
        Bouquets = _productRepository.GetBouquetsByGenre(selectedGenre);
    }

    // OnPost kører når brugeren trykker Add to Bag
    public IActionResult OnPost(int productId)
    {
        // Finder det valgte produkt i databasen
        Product? product = _productRepository.GetById(productId);

        // Kontrollerer om produktet findes
        if (product == null)
        {
            // Sender brugeren tilbage hvis produktet ikke findes
            return RedirectToPage("/Products");
        }

        // Kontrollerer om produktet er udsolgt
        if (product.StockQuantity <= 0)
        {
            // Sender brugeren tilbage hvis lageret er tomt
            return RedirectToPage("/Products");
        }

        // Henter brugeren som er logget ind
        User? user = _sessionService.GetCurrentUser();

        // Hvis ingen er logget ind bruges gæstekurven
        if (user == null)
        {
            // Henter gæstens indkøbskurv fra session
            List<ShoppingCartProduct> guestCart = _sessionService.GetGuestCart();

            // Finder produktet i gæstekurven hvis det allerede ligger der
            ShoppingCartProduct? existingProduct = guestCart.FirstOrDefault(
                item => item.ProductId == productId);

            // Finder hvor mange gæsten allerede har af produktet
            // Bruger nul hvis produktet ikke findes i kurven
            int currentQuantity = existingProduct?.Quantity ?? 0;

            // Kontrollerer om der stadig er flere på lager
            if (currentQuantity < product.StockQuantity)
            {
                // Tilføjer ét produkt til gæstekurven
                _sessionService.AddToGuestCart(productId, 1);
            }

            // Sender gæsten til indkøbskurven
            return RedirectToPage("/Cart");
        }

        // Kontrollerer om brugeren er Admin
        if (user is Admin)
        {
            // Sender admin tilbage da admin ikke må bruge kundekurven
            return RedirectToPage("/Products");
        }

        // Kontrollerer om brugeren er en Customer
        if (user is not Customer customer)
        {
            // Sender andre brugertyper tilbage til produktsiden
            return RedirectToPage("/Products");
        }

        // Henter kundens id
        int customerId = customer.Id;

        // Finder kundens indkøbskurv i databasen
        ShoppingCart? shoppingCart = _shoppingCartRepository.GetByCustomerId(customerId);

        // Kontrollerer om kunden allerede har en kurv
        if (shoppingCart == null)
        {
            // Opretter en ny kurv og henter dens id
            int shoppingCartId = _shoppingCartRepository.Create(customerId);

            // Opretter et ShoppingCart objekt med det nye id
            shoppingCart = new ShoppingCart
            {
                // Gemmer kurvens id
                Id = shoppingCartId,

                // Gemmer id på kunden som ejer kurven
                CustomerId = customerId
            };
        }

        // Finder hvor mange kunden allerede har af produktet i kurven
        int quantity = _shoppingCartRepository.GetProductQuantity(
            shoppingCart.Id, productId);

        // Kontrollerer om produktet allerede ligger i kurven
        if (quantity == 0)
        {
            // Tilføjer ét produkt hvis det ikke allerede findes
            _shoppingCartRepository.AddProduct(shoppingCart.Id, productId, 1);
        }
        else
        {
            // Kontrollerer om der er plads til endnu et produkt
            if (quantity < product.StockQuantity)
            {
                // Lægger én til det eksisterende antal
                quantity = quantity + 1;

                // Gemmer det nye antal i kundens databasekurv
                _shoppingCartRepository.UpdateQuantity(
                    shoppingCart.Id, productId, quantity);
            }
        }

        // Sender kunden til indkøbskurven
        return RedirectToPage("/Cart");
    }

    // Hjælpemetode som finder billedet til en bestemt genre
    public string GetCollectionImage(BookGenre genre)
    {
        // Kontrollerer om genren er Romance
        if (genre == BookGenre.Romance)
        {
            // Returnerer billedet til Romance
            return "/Images/romanceall.png";
        }

        // Kontrollerer om genren er Fantasy
        if (genre == BookGenre.Fantasy)
        {
            // Returnerer billedet til Fantasy
            return "/Images/fantasyall.png";
        }

        // Kontrollerer om genren er Crime
        if (genre == BookGenre.Crime)
        {
            // Returnerer billedet til Crime
            return "/Images/crimeall.png";
        }

        // Kontrollerer om genren er Biography
        if (genre == BookGenre.Biography)
        {
            // Returnerer billedet til Biography
            return "/Images/Bioall.png";
        }

        // Returnerer billedet til Dark Romance hvis ingen af de andre matcher
        return "/Images/DRALL.png";
    }

    // Hjælpemetode som bestemmer hvordan genren skal skrives
    public string GetGenreName(BookGenre genre)
    {
        // Kontrollerer om genren er DarkRomance
        if (genre == BookGenre.DarkRomance)
        {
            // Returnerer navnet med mellemrum
            return "Dark Romance";
        }

        // Laver de andre genrer om til tekst
        return genre.ToString();
    }
}
