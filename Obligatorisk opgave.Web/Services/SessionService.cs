using Microsoft.AspNetCore.Http;
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

    // Henter sessionen fra den aktuelle HTTP-request
    private ISession GetSession()
    {
        ISession? session = _httpContextAccessor.HttpContext?.Session;

        if (session == null)
        {
            throw new InvalidOperationException(
                "Sessionen er ikke tilgængelig.");
        }

        return session;
    }
}