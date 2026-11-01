using Microsoft.AspNetCore.Http;
using System.Text.Json;
using Obligatoris.opgave.Domain.Interfaces;
using Obligatoris.opgave.Domain.Models;

namespace Obligatorisk_opgave.Services;

public class SessionService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserRepository _userRepository;

    // Nøglerne bruges til at finde værdierne i sessionen
    private const string UserIdKey = "UserId";
    private const string UserTypeKey = "UserType";
    private const string GuestCartKey = "GuestCart";

    public SessionService(
        IHttpContextAccessor httpContextAccessor,
        IUserRepository userRepository)
    {
        _httpContextAccessor = httpContextAccessor;
        _userRepository = userRepository;
    }

    // Opretter en session efter et korrekt login
    public void CreateSession(User user)
    {
        ISession session = GetSession();

        // Vi gemmer ikke PasswordHash eller andre følsomme oplysninger
        session.SetInt32(UserIdKey, user.Id);

        // Brugerens type bestemmes ud fra arv
        if (user is Customer)
        {
            session.SetString(UserTypeKey, "Customer");
        }
        else if (user is Admin)
        {
            session.SetString(UserTypeKey, "Admin");
        }
    }

    // Henter den aktuelle bruger fra databasen
    public User? GetCurrentUser()
    {
        int? userId = GetUserId();

        // Der er ingen bruger i sessionen
        if (userId == null)
        {
            return null;
        }

        User? user = _userRepository.GetById(userId.Value);

        // En deaktiveret bruger må ikke behandles som logget ind
        if (user == null || !user.IsActive)
        {
            return null;
        }

        return user;
    }

    // Henter ID'et på den bruger, der er logget ind
    public int? GetUserId()
    {
        return GetSession().GetInt32(UserIdKey);
    }

    // Returnerer enten Customer eller Admin
    public string? GetUserRole()
    {
        return GetSession().GetString(UserTypeKey);
    }

    // Kontrollerer, om der findes en aktiv bruger
    public bool IsLoggedIn()
    {
        return GetCurrentUser() != null;
    }

    // Fjerner alle oplysninger fra sessionen
    public void Logout()
    {
        GetSession().Clear();
    }
        
    // Henter gæstens indkøbskurv fra sessionen
    public List<ShoppingCartProduct> GetGuestCart()
    {
        string? cartJson = GetSession().GetString(GuestCartKey);

        // Hvis der ikke findes en kurv endnu, returneres en tom liste
        if (string.IsNullOrWhiteSpace(cartJson))
        {
            return new List<ShoppingCartProduct>();
        }

        List<ShoppingCartProduct>? cart =
            JsonSerializer.Deserialize<List<ShoppingCartProduct>>(
                cartJson);

        return cart ?? new List<ShoppingCartProduct>();
    }


    // Tilføjer et produkt til gæstens kurv
    public void AddToGuestCart( int productId, int quantity)
    {
        // Ugyldige værdier bliver ikke gemt
        if (productId <= 0 || quantity <= 0)
        {
            return;
        }

        List<ShoppingCartProduct> cart = GetGuestCart();

        ShoppingCartProduct? existingProduct = cart.FirstOrDefault(
                product => product.ProductId == productId);

        if (existingProduct == null)
        {
            // Produktet findes ikke i kurven endnu
            cart.Add(
                new ShoppingCartProduct
                {
                    ProductId = productId,
                    Quantity = quantity
                });
        }
        else
        {
            // Produktet findes allerede, så antallet bliver lagt sammen
            existingProduct.Quantity += quantity;
        }

        SaveGuestCart(cart);
    }


    // Ændrer antallet af et produkt i gæstens kurv
    public void UpdateGuestCartQuantity( int productId, int quantity)
    {
        List<ShoppingCartProduct> cart = GetGuestCart();

        ShoppingCartProduct? product = 
            cart.FirstOrDefault(item => item.ProductId == productId);

        if (product == null)
        {
            return;
        }

        // Et antal på 0 eller mindre fjerner produktet
        if (quantity <= 0)
        {
            cart.Remove(product);
        }
        else
        {
            product.Quantity = quantity;
        }

        SaveGuestCart(cart);
    }


    // Fjerner et produkt fra gæstens kurv
    public void RemoveFromGuestCart(int productId)
    {
        List<ShoppingCartProduct> cart = GetGuestCart();

        ShoppingCartProduct? product =
            cart.FirstOrDefault(item => item.ProductId == productId);

        if (product == null)
        {
            return;
        }
        cart.Remove(product);
        SaveGuestCart(cart);
    }
    
    // Fjerner hele gæstens kurv
    public void ClearGuestCart()
    {
        GetSession().Remove(GuestCartKey);
    }


    // Gemmer gæstens kurv som JSON i sessionen
    private void SaveGuestCart(List<ShoppingCartProduct> cart)
    {
        string cartJson = JsonSerializer.Serialize(cart);

        GetSession().SetString(GuestCartKey, cartJson);
    }

        
    
    // Henter sessionen fra den aktuelle HTTP-request
    private ISession GetSession()
    {
        ISession? session = _httpContextAccessor.HttpContext?.Session;

        if (session == null)
        {
            throw new InvalidOperationException("Sessionen er ikke tilgængelig.");
        }

        return session;
    }
}