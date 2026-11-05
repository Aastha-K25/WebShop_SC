using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Obligatoris.opgave.Domain.Interfaces;
using Obligatoris.opgave.Domain.Models;
using Obligatorisk_opgave.Services;

namespace Obligatorisk_opgave.Pages;

public class AdminProducts : PageModel
{
    private readonly IProductRepository _productRepository;
    private readonly SessionService _sessionService;
    private readonly AuthorizationService _authorizationService;


    public AdminProducts(
        IProductRepository productRepository,
        SessionService sessionService,
        AuthorizationService authorizationService)
    {
        _productRepository = productRepository;
        _sessionService = sessionService;
        _authorizationService = authorizationService;
    }


    public List<Product> Products { get; set; }
        = new List<Product>();


    [BindProperty]
    public Bouquet Bouquet { get; set; }
    = new Bouquet
    {
        Name = "",
        FlowerType = ""
    };


    public IActionResult OnGet()
    {
        User? user =
            _sessionService.GetCurrentUser();


        if (!_authorizationService.CanManageProducts(user))
        {
            return Forbid();
        }


        Products =
            _productRepository.GetAll();


        return Page();
    }


    // Opretter nyt produkt
    public IActionResult OnPostCreate()
    {
        User? user =
            _sessionService.GetCurrentUser();


        if (!_authorizationService.CanManageProducts(user))
        {
            return Forbid();
        }


        // NumberOfBooks bestemmes ud fra BouquetSize
        Bouquet.NumberOfBooks =
            GetNumberOfBooks(
                Bouquet.BouquetSize);


        if (!IsValidBouquet(Bouquet))
        {
            Products =
                _productRepository.GetAll();

            return Page();
        }


        _productRepository.Add(
            Bouquet);


        return RedirectToPage(
            "/AdminProducts");
    }


    // Opdaterer eksisterende produkt
    public IActionResult OnPostUpdate()
    {
        User? user =
            _sessionService.GetCurrentUser();


        if (!_authorizationService.CanManageProducts(user))
        {
            return Forbid();
        }


        Product? existingProduct =
            _productRepository.GetById(
                Bouquet.Id);


        if (existingProduct == null)
        {
            return NotFound();
        }


        // NumberOfBooks bestemmes ud fra BouquetSize
        Bouquet.NumberOfBooks =
            GetNumberOfBooks(
                Bouquet.BouquetSize);


        if (!IsValidBouquet(Bouquet))
        {
            Products =
                _productRepository.GetAll();

            return Page();
        }


        _productRepository.Update(
            Bouquet);


        return RedirectToPage(
            "/AdminProducts");
    }


    // Sletter produkt
    public IActionResult OnPostDelete(
        int id)
    {
        User? user =
            _sessionService.GetCurrentUser();


        if (!_authorizationService.CanManageProducts(user))
        {
            return Forbid();
        }


        Product? product =
            _productRepository.GetById(
                id);


        if (product == null)
        {
            return NotFound();
        }


        _productRepository.Delete(
            id);


        return RedirectToPage(
            "/AdminProducts");
    }


    // Bestemmer antal b�ger ud fra st�rrelsen
    private int GetNumberOfBooks(
        BouquetSize bouquetSize)
    {
        if (bouquetSize == BouquetSize.Small)
        {
            return 2;
        }


        if (bouquetSize == BouquetSize.Medium)
        {
            return 3;
        }


        return 5;
    }


    // Simpel server-side validering
    private bool IsValidBouquet(
        Bouquet bouquet)
    {
        if (string.IsNullOrWhiteSpace(
                bouquet.Name))
        {
            return false;
        }


        if (string.IsNullOrWhiteSpace(
                bouquet.FlowerType))
        {
            return false;
        }


        if (bouquet.Price < 0)
        {
            return false;
        }


        if (bouquet.StockQuantity < 0)
        {
            return false;
        }


        // Kontrollerer, at genren findes i BookGenre.
        if (!Enum.IsDefined(
                typeof(BookGenre),
                bouquet.BookGenre))
        {
            return false;
        }


        // Kontrollerer, at størrelsen findes i BouquetSize.
        if (!Enum.IsDefined(
                typeof(BouquetSize),
                bouquet.BouquetSize))
        {
            return false;
        }


        if (bouquet.NumberOfBooks <= 0)
        {
            return false;
        }


        return true;
    }
}
