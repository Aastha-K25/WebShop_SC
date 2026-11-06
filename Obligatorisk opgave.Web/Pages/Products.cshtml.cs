using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Obligatoris.opgave.Domain.Interfaces;
using Obligatoris.opgave.Domain.Models;
using Obligatorisk_opgave.Services;

namespace Obligatorisk_opgave.Pages;

public class Products : PageModel
{
    private readonly IProductRepository _productRepository;
    private readonly IShoppingCartRepository _shoppingCartRepository;
    private readonly SessionService _sessionService;


    // Henter repositories og session service
    public Products(
        IProductRepository productRepository,
        IShoppingCartRepository shoppingCartRepository,
        SessionService sessionService)
    {
        _productRepository = productRepository;
        _shoppingCartRepository = shoppingCartRepository;
        _sessionService = sessionService;
    }


    public List<Bouquet> Bouquets { get; set; } =
        new List<Bouquet>();


    public List<Bouquet> PopularBouquets { get; set; } =
        new List<Bouquet>();


    public BookGenre? SelectedGenre { get; set; }

    public string SearchTerm { get; set; } = string.Empty;

    // Henter produkter n�r siden �bnes
    public void OnGet(string? genre, string? search)
    {
        PopularBouquets =
            _productRepository.GetPopularBouquets(4);

        // Begrænser inputlængden og søger på serveren
        SearchTerm = search?.Trim() ?? string.Empty;

        if (SearchTerm.Length > 200)
        {
            SearchTerm = SearchTerm[..200];
        }

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            Bouquets = _productRepository.SearchBouquets(SearchTerm);
            return;
        }


        // Viser alle hvis der ikke er valgt genre
        if (string.IsNullOrWhiteSpace(genre))
        {
            Bouquets =
                _productRepository.GetAllBouquets();

            return;
        }


        BookGenre selectedGenre;


        bool genreExists =
            Enum.TryParse(
                genre,
                true,
                out selectedGenre);


        
        // Viser alle, hvis teksten ikke kan omdannes til en genre, eller hvis værdien ikke findes i BookGenre.
        if (!genreExists ||
            !Enum.IsDefined(
                typeof(BookGenre),
                selectedGenre))
        {
            Bouquets =
                _productRepository.GetAllBouquets();

            return;
        }

        SelectedGenre =
            selectedGenre;


        Bouquets =
            _productRepository.GetBouquetsByGenre(
                selectedGenre);
    }


    // Add to Bag
    public IActionResult OnPost(
        int productId)
    {
        // Finder produktet
        Product? product =
            _productRepository.GetById(
                productId);


        // Stopper hvis produktet ikke findes
        if (product == null)
        {
            return RedirectToPage(
                "/Products");
        }


        // Stopper hvis produktet er udsolgt
        if (product.StockQuantity <= 0)
        {
            return RedirectToPage(
                "/Products");
        }


        // Finder den bruger der er logget ind
        User? user =
            _sessionService.GetCurrentUser();


        // GUEST
        // En g�st har ingen bruger i sessionen
        if (user == null)
        {
            List<ShoppingCartProduct> guestCart =
                _sessionService.GetGuestCart();


            ShoppingCartProduct? existingProduct =
                guestCart.FirstOrDefault(
                    item => item.ProductId == productId);


            int currentQuantity =
                existingProduct?.Quantity ?? 0;


            // M� ikke l�gge flere i kurven end der er p� lager
            if (currentQuantity < product.StockQuantity)
            {
                _sessionService.AddToGuestCart(
                    productId,
                    1);
            }


            return RedirectToPage(
                "/Cart");
        }


        // ADMIN
        // Admin m� ikke bruge customer cart
        if (user is Admin)
        {
            return RedirectToPage(
                "/Products");
        }


        // CUSTOMER
        if (user is not Customer customer)
        {
            return RedirectToPage(
                "/Products");
        }


        int customerId =
            customer.Id;


        ShoppingCart? shoppingCart =
            _shoppingCartRepository.GetByCustomerId(
                customerId);


        // Opretter kurv hvis kunden ikke har en
        if (shoppingCart == null)
        {
            int shoppingCartId =
                _shoppingCartRepository.Create(
                    customerId);


            shoppingCart =
                new ShoppingCart
                {
                    Id = shoppingCartId,
                    CustomerId = customerId
                };
        }


        int quantity =
            _shoppingCartRepository.GetProductQuantity(
                shoppingCart.Id,
                productId);


        // Produktet findes ikke i kurven endnu
        if (quantity == 0)
        {
            _shoppingCartRepository.AddProduct(
                shoppingCart.Id,
                productId,
                1);
        }
        else
        {
            // M� ikke overstige lageret
            if (quantity < product.StockQuantity)
            {
                quantity =
                    quantity + 1;


                _shoppingCartRepository.UpdateQuantity(
                    shoppingCart.Id,
                    productId,
                    quantity);
            }
        }


        return RedirectToPage(
            "/Cart");
    }

    // Finder billede til collection
    public string GetCollectionImage(
        BookGenre genre)
    {
        if (genre == BookGenre.Romance)
        {
            return "/Images/romanceall.png";
        }


        if (genre == BookGenre.Fantasy)
        {
            return "/Images/fantasyall.png";
        }


        if (genre == BookGenre.Crime)
        {
            return "/Images/crimeall.png";
        }


        if (genre == BookGenre.Biography)
        {
            return "/Images/Bioall.png";
        }


        return "/Images/DRALL.png";
    }


    // Skriver Dark Romance med mellemrum
    public string GetGenreName(
        BookGenre genre)
    {
        if (genre == BookGenre.DarkRomance)
        {
            return "Dark Romance";
        }


        return genre.ToString();
    }
}