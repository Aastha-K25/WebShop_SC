
using Microsoft.AspNetCore.Http; // Bruges til at arbejde med HTTP og session
using System.Text.Json; // Bruges til at lave objekter om til JSON og tilbage igen
using Obligatoris.opgave.Domain.Interfaces; // Giver adgang til vores interfaces
using Obligatoris.opgave.Domain.Models; // Giver adgang til vores modeller

namespace Obligatorisk_opgave.Services;

// Service som håndterer brugerens session og gæstens indkøbskurv
public class SessionService
{
    // Giver adgang til sessionen fra den aktuelle HTTP request
    private readonly IHttpContextAccessor _httpContextAccessor;

    // Bruges til at hente brugere fra databasen
    private readonly IUserRepository _userRepository;

    // Nøgle som bruges til at gemme brugerens id i sessionen
    private const string UserIdKey = "UserId";

    // Nøgle som bruges til at gemme brugerens type
    private const string UserTypeKey = "UserType";

    // Nøgle som bruges til at gemme gæstens indkøbskurv
    private const string GuestCartKey = "GuestCart";

    // Constructor som modtager vores HTTP context accessor og repository
    public SessionService(
        IHttpContextAccessor httpContextAccessor,
        IUserRepository userRepository)
    {
        // Gemmer vores HTTP context accessor
        _httpContextAccessor = httpContextAccessor;

        // Gemmer vores user repository
        _userRepository = userRepository;
    }

    // Opretter en session når brugeren er logget ind
    public void CreateSession(User user)
    {
        // Henter den aktuelle session
        ISession session = GetSession();

        // Gemmer kun brugerens id og ikke password eller password hash
        session.SetInt32(UserIdKey, user.Id);

        // Kontrollerer om brugeren er en Customer
        if (user is Customer)
        {
            // Gemmer brugertypen Customer i sessionen
            session.SetString(UserTypeKey, "Customer");
        }
        // Kontrollerer om brugeren er en Admin
        else if (user is Admin)
        {
            // Gemmer brugertypen Admin i sessionen
            session.SetString(UserTypeKey, "Admin");
        }
    }

    // Henter den bruger som er logget ind
    public User? GetCurrentUser()
    {
        // Henter brugerens id fra sessionen
        int? userId = GetUserId();

        // Kontrollerer om der findes et bruger id i sessionen
        if (userId == null)
        {
            // Returnerer null hvis ingen er logget ind
            return null;
        }

        // Finder brugeren i databasen ud fra id
        // Value henter selve tallet fra den nullable variabel
        User? user = _userRepository.GetById(userId.Value);

        // Kontrollerer om brugeren findes og stadig er aktiv
        if (user == null || !user.IsActive)
        {
            // Returnerer null hvis brugeren ikke findes eller er deaktiveret
            return null;
        }

        // Returnerer den aktuelle bruger fra databasen
        return user;
    }

    // Henter id på brugeren som er logget ind
    public int? GetUserId()
    {
        // Henter brugerens id fra sessionen
        // Returnerer null hvis der ikke er gemt et id
        return GetSession().GetInt32(UserIdKey);
    }

    // Henter brugertypen fra sessionen
    public string? GetUserRole()
    {
        // Returnerer Customer eller Admin hvis værdien findes
        return GetSession().GetString(UserTypeKey);
    }

    // Kontrollerer om der er en aktiv bruger logget ind
    public bool IsLoggedIn()
    {
        // Returnerer true hvis GetCurrentUser finder en aktiv bruger
        return GetCurrentUser() != null;
    }

    // Logger brugeren ud ved at rydde sessionen
    public void Logout()
    {
        // Fjerner alle oplysninger som er gemt i sessionen
        GetSession().Clear();
    }

    // Henter gæstens indkøbskurv fra sessionen
    public List<ShoppingCartProduct> GetGuestCart()
    {
        // Henter den gemte kurv som JSON tekst
        string? cartJson = GetSession().GetString(GuestCartKey);

        // Kontrollerer om der findes en gæstekurv
        if (string.IsNullOrWhiteSpace(cartJson))
        {
            // Returnerer en tom liste hvis kurven ikke findes
            return new List<ShoppingCartProduct>();
        }

        // Laver JSON teksten tilbage til en liste med produkter
        List<ShoppingCartProduct>? cart =
            JsonSerializer.Deserialize<List<ShoppingCartProduct>>(cartJson);

        // Returnerer kurven eller en tom liste hvis resultatet er null
        return cart ?? new List<ShoppingCartProduct>();
    }

    // Tilføjer et produkt til gæstens indkøbskurv
    public void AddToGuestCart(int productId, int quantity)
    {
        // Kontrollerer om produkt id og antal er gyldige
        if (productId <= 0 || quantity <= 0)
        {
            // Stopper hvis værdierne er nul eller negative
            return;
        }

        // Henter gæstens nuværende indkøbskurv
        List<ShoppingCartProduct> cart = GetGuestCart();

        // Søger efter produktet i kurven ud fra dets id
        // FirstOrDefault returnerer null hvis produktet ikke findes
        ShoppingCartProduct? existingProduct = cart.FirstOrDefault(
            product => product.ProductId == productId);

        // Kontrollerer om produktet allerede ligger i kurven
        if (existingProduct == null)
        {
            // Opretter et nyt ShoppingCartProduct objekt
            cart.Add(
                new ShoppingCartProduct
                {
                    // Gemmer produktets id
                    ProductId = productId,

                    // Gemmer hvor mange der skal tilføjes
                    Quantity = quantity
                });
        }
        else
        {
            // Lægger det nye antal til det eksisterende antal
            existingProduct.Quantity += quantity;
        }

        // Gemmer den opdaterede gæstekurv i sessionen
        SaveGuestCart(cart);
    }

    // Ændrer antallet af et produkt i gæstens indkøbskurv
    public void UpdateGuestCartQuantity(int productId, int quantity)
    {
        // Henter gæstens indkøbskurv
        List<ShoppingCartProduct> cart = GetGuestCart();

        // Finder det produkt som skal opdateres
        ShoppingCartProduct? product = cart.FirstOrDefault(
            item => item.ProductId == productId);

        // Kontrollerer om produktet findes i kurven
        if (product == null)
        {
            // Stopper hvis produktet ikke findes
            return;
        }

        // Kontrollerer om det nye antal er nul eller mindre
        if (quantity <= 0)
        {
            // Fjerner produktet fra kurven
            cart.Remove(product);
        }
        else
        {
            // Gemmer det nye antal på produktet
            product.Quantity = quantity;
        }

        // Gemmer den opdaterede kurv i sessionen
        SaveGuestCart(cart);
    }

    // Fjerner et bestemt produkt fra gæstens indkøbskurv
    public void RemoveFromGuestCart(int productId)
    {
        // Henter gæstens indkøbskurv
        List<ShoppingCartProduct> cart = GetGuestCart();

        // Finder produktet som skal fjernes
        ShoppingCartProduct? product = cart.FirstOrDefault(
            item => item.ProductId == productId);

        // Kontrollerer om produktet findes i kurven
        if (product == null)
        {
            // Stopper hvis produktet ikke findes
            return;
        }

        // Fjerner produktet fra listen
        cart.Remove(product);

        // Gemmer den opdaterede kurv i sessionen
        SaveGuestCart(cart);
    }

    // Tømmer hele gæstens indkøbskurv
    public void ClearGuestCart()
    {
        // Fjerner gæstekurven fra sessionen
        GetSession().Remove(GuestCartKey);
    }

    // Hjælpemetode som gemmer gæstekurven i sessionen
    private void SaveGuestCart(List<ShoppingCartProduct> cart)
    {
        // Laver listen med produkter om til JSON tekst
        string cartJson = JsonSerializer.Serialize(cart);

        // Gemmer JSON teksten i sessionen
        GetSession().SetString(GuestCartKey, cartJson);
    }

    // Hjælpemetode som henter den aktuelle session
    private ISession GetSession()
    {
        // Henter sessionen fra den aktuelle HTTP request
        // Spørgsmålstegnet betyder at HttpContext gerne må være null
        ISession? session = _httpContextAccessor.HttpContext?.Session;

        // Kontrollerer om sessionen findes
        if (session == null)
        {
            // Stopper med en fejl hvis sessionen ikke er tilgængelig
            throw new InvalidOperationException("Sessionen er ikke tilgængelig.");
        }

        // Returnerer sessionen så de andre metoder kan bruge den
        return session;
    }
}
