using Microsoft.Extensions.Caching.Memory;
using Obligatoris.opgave.Domain.Models;
using Obligatoris.opgave.Domain.Interfaces;
using Obligatorisk_opgave.Security;

namespace Obligatorisk_opgave.Services;

public class AuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly PasswordHasher _passwordHasher;
    private readonly SessionService _sessionService;
    private readonly IMemoryCache _memoryCache;
    
    private const int MaximumLoginAttempts = 5;

    public AuthenticationService(IUserRepository userRepository, PasswordHasher passwordHasher,
        SessionService sessionService, IMemoryCache memoryCache)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _sessionService = sessionService;
        _memoryCache = memoryCache;
    }
    
    //Opretter en ny kunde. Null betyder at oprettelsen lykkedes
    public string? RegisterCustomer(string name, string email, string password, string address)
    {
        name = name.Trim();
        email = email.Trim();
        address = address.Trim();
        
        //Alle felter skal være udfyldt 
        if (string.IsNullOrWhiteSpace(name) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(address))
        {
            return "Please fill out all fields.";
        }
        
        if (_userRepository.EmailExists(email))
        {
            return "An account with this email already exists.";
        }
        
        //Kontrollerer passwordet ud fra vores password policy
        string? passwordError = PasswordValidator.Validate(password, name, email);

        if (passwordError != null)
        {
            return passwordError;
        }

        string passwordHash = _passwordHasher.HashPassword(password);

        Customer customer = new Customer
        {
            Name = name,
            Email = email,
            Address = address,
            PasswordHash = passwordHash,
            IsActive = true
        };
        _userRepository.Add(customer);

        return null;
    }
    
     // Logger en bruger ind
    // Null betyder, at login lykkedes
    public string? Login(string email, string password)
    {
        string normalizedEmail =
            email.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(normalizedEmail) ||
            string.IsNullOrWhiteSpace(password))
        {
            return "Please enter email and password.";
        }

        // Kontrollerer om login midlertidigt er blokeret
        if (IsLoginBlocked(normalizedEmail))
        {
            return "Too many failed login attempts. Please try again later.";
        }

        User? user =
            _userRepository.GetByEmail(normalizedEmail);

        // Samme besked bruges ved ukendt e-mail og forkert password.
        // På den måde afslører vi ikke, om en bestemt konto findes.
        if (user == null || !user.IsActive)
        {
            RegisterFailedLogin(normalizedEmail);
            return "Invalid email or password.";
        }

        bool passwordIsCorrect =
            _passwordHasher.VerifyPassword(
                password,
                user.PasswordHash);

        if (!passwordIsCorrect)
        {
            RegisterFailedLogin(normalizedEmail);
            return "Invalid email or password.";
        }

        // Et korrekt login nulstiller fejltælleren
        _memoryCache.Remove(GetLoginAttemptKey(normalizedEmail));

        // Sessionen oprettes først efter korrekt authentication
        _sessionService.CreateSession(user);

        return null;
    }

    // Returnerer den bruger, der er logget ind
    public User? GetCurrentUser()
    {
        return _sessionService.GetCurrentUser();
    }

    // Kontrollerer om en bruger er logget ind
    public bool IsAuthenticated()
    {
        return _sessionService.IsLoggedIn();
    }

    // Rydder brugerens session
    public void Logout()
    {
        _sessionService.Logout();
    }
    
    // Ændrer passwordet for den kunde, der er logget ind. Null betyder, at passwordet blev ændret
    public string? ChangePassword(
        string currentPassword,
        string newPassword)
    {
        User? currentUser =
            _sessionService.GetCurrentUser();

        // Brugeren skal være logget ind som Customer
        if (currentUser is not Customer customer)
        {
            return "You must be logged in as a customer.";
        }

        // Det nuværende password skal være korrekt
        bool currentPasswordIsCorrect =
            _passwordHasher.VerifyPassword(
                currentPassword,
                customer.PasswordHash);

        if (!currentPasswordIsCorrect)
        {
            return "The current password is incorrect.";
        }

        // Det nye password må ikke være det samme som det gamle
        bool passwordIsUnchanged =
            _passwordHasher.VerifyPassword(
                newPassword,
                customer.PasswordHash);

        if (passwordIsUnchanged)
        {
            return "The new password must be different from the current password.";
        }

        // Det nye password skal følge den samme password policy
        string? passwordError =
            PasswordValidator.Validate(
                newPassword,
                customer.Name,
                customer.Email);

        if (passwordError != null)
        {
            return passwordError;
        }

        // Det nye password hashes før opdatering
        customer.PasswordHash =
            _passwordHasher.HashPassword(newPassword);

        _userRepository.Update(customer);

        return null;
    }

    // Registrerer et fejlet loginforsøg
    private void RegisterFailedLogin(string email)
    {
        string cacheKey = GetLoginAttemptKey(email);

        LoginAttempt loginAttempt =
            _memoryCache.Get<LoginAttempt>(cacheKey)
            ?? new LoginAttempt();

        loginAttempt.NumberOfAttempts++;

        // Kontoens login begrænses midlertidigt efter fem forsøg
        if (loginAttempt.NumberOfAttempts >= MaximumLoginAttempts)
        {
            loginAttempt.BlockedUntil =
                DateTime.UtcNow.AddMinutes(15);
        }

        // Oplysningerne fjernes automatisk efter 15 minutter
        _memoryCache.Set(
            cacheKey,
            loginAttempt,
            TimeSpan.FromMinutes(15));
    }

    // Kontrollerer om login er midlertidigt blokeret
    private bool IsLoginBlocked(string email)
    {
        LoginAttempt? loginAttempt =
            _memoryCache.Get<LoginAttempt>(
                GetLoginAttemptKey(email));

        if (loginAttempt?.BlockedUntil == null)
        {
            return false;
        }

        return loginAttempt.BlockedUntil > DateTime.UtcNow;
    }

    // Laver en unik nøgle til fejlede loginforsøg
    private static string GetLoginAttemptKey(string email)
    {
        return $"LoginAttempt:{email}";
    }

    // Gemmer kun midlertidige oplysninger om loginforsøg
    private class LoginAttempt
    {
        public int NumberOfAttempts { get; set; }
        public DateTime? BlockedUntil { get; set; }
    }
}