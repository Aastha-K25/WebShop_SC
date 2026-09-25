using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Obligatoris.opgave.Domain.Interfaces;
using Obligatoris.opgave.Domain.Models;

namespace Obligatorisk_opgave.Pages;

public class Products : PageModel
{
    private readonly IProductRepository _productRepository;
    private readonly IShoppingCartRepository _shoppingCartRepository;


    // Henter repositories
    public Products(
        IProductRepository productRepository,
        IShoppingCartRepository shoppingCartRepository)
    {
        _productRepository = productRepository;
        _shoppingCartRepository = shoppingCartRepository;
    }


    public List<Bouquet> Bouquets { get; set; } =
        new List<Bouquet>();


    public List<Bouquet> PopularBouquets { get; set; } =
        new List<Bouquet>();


    public BookGenre? SelectedGenre { get; set; }


    // Henter produkter når siden åbnes
    public void OnGet(string? genre)
    {
        PopularBouquets =
            _productRepository.GetPopularBouquets(4);


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


        // Viser alle hvis genren ikke findes
        if (!genreExists)
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
        // Midlertidigt customer id indtil login er lavet
        int customerId = 1;


        Product? product =
            _productRepository.GetById(
                productId);


        if (product == null)
        {
            return RedirectToPage(
                "/Products");
        }


        if (product.StockQuantity <= 0)
        {
            return RedirectToPage(
                "/Products");
        }


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


        if (quantity == 0)
        {
            _shoppingCartRepository.AddProduct(
                shoppingCart.Id,
                productId,
                1);
        }
        else
        {
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