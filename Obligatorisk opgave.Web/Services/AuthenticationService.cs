
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Obligatoris.opgave.Domain.Models;
using Obligatoris.opgave.Domain.Interfaces;
using Obligatorisk_opgave.Security;
using System.Security.Cryptography;

namespace Obligatorisk_opgave.Services;

public class AuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly PasswordHasher _passwordHasher;
    private readonly SessionService _sessionService;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;

    private const int MaximumLoginAttempts = 5;


    public AuthenticationService(
        IUserRepository userRepository,
        IPasswordResetTokenRepository passwordResetTokenRepository,
        PasswordHasher passwordHasher,
        SessionService sessionService,
        IMemoryCache memoryCache,
        ILogger<AuthenticationService> logger)
    {
        _userRepository = userRepository;
        _passwordResetTokenRepository = passwordResetTokenRepository;
        _passwordHasher = passwordHasher;
        _sessionService = sessionService;
        _memoryCache = memoryCache;
        _logger = logger;
    }


    // Opretter en ny kunde.
    // Null betyder at oprettelsen lykkedes.
    public string? RegisterCustomer(
        string name,
        string surname,
        string email,
        string password,
        string address)
    {
        name = name.Trim();

        surname = surname.Trim();

        email =
            email
                .Trim()
                .ToLowerInvariant();

        address = address.Trim();


        // Alle felter skal være udfyldt
        if (string.IsNullOrWhiteSpace(name) ||
            string.IsNullOrWhiteSpace(surname) ||
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


        // Kontrollerer passwordet ud fra vores password policy
        string? passwordError =
            PasswordValidator.Validate(
                password,
                name,
                email);


        if (passwordError != null)
        {
            return passwordError;
        }


        string passwordHash =
            _passwordHasher.HashPassword(password);


        Customer customer =
            new Customer
            {
                Name = name,
                Surname = surname,
                Email = email,
                Address = address,
                PasswordHash = passwordHash,
                IsActive = true
            };


        _userRepository.Add(customer);

        return null;
    }


    // Logger en bruger ind.
    // Null betyder, at login lykkedes.
    public string? Login(  string email,string password)
    {
        string normalizedEmail =
            email
                .Trim()
                .ToLowerInvariant();


        if (string.IsNullOrWhiteSpace(normalizedEmail) ||
            string.IsNullOrWhiteSpace(password))
        {
            return "Please enter email and password.";
        }


        // Kontrollerer om login midlertidigt er blokeret
        if (IsLoginBlocked(normalizedEmail))
        {
            _logger.LogWarning(
                "Login attempt blocked due to too many failed attempts.");

            return "Too many failed login attempts. Please try again later.";
        }


        User? user =
            _userRepository.GetByEmail(
                normalizedEmail);


        // Samme besked bruges ved ukendt e-mail og forkert password.
        // På den måde afslører vi ikke, om en bestemt konto findes.
        if (user == null ||
            !user.IsActive)
        {
            RegisterFailedLogin(
                normalizedEmail);

            return "Invalid email or password.";
        }


        bool passwordIsCorrect =
            _passwordHasher.VerifyPassword(
                password,
                user.PasswordHash);


        if (!passwordIsCorrect)
        {
            RegisterFailedLogin(
                normalizedEmail);

            return "Invalid email or password.";
        }


        // MFA er forberedt, men ikke implementeret i skoleprojektet.
        // En Admin med aktiveret MFA må derfor ikke få en almindelig
        // session uden først at gennemføre en MFA-kontrol.
        if (user is Admin admin &&
            admin.MfaEnabled)
        {
            return "MFA is required for this account, but the MFA flow is not available yet.";
        }


        // Et korrekt login nulstiller fejltælleren
        _memoryCache.Remove(
            GetLoginAttemptKey(
                normalizedEmail));


        // Sessionen oprettes først efter korrekt authentication
        _sessionService.CreateSession(
            user);


        // Registrerer et vellykket login uden password
        _logger.LogInformation(
            "User successfully authenticated. UserId: {UserId}",
            user.Id);


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


    // Ændrer passwordet for den kunde, der er logget ind.
    // Null betyder, at passwordet blev ændret.
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
            _passwordHasher.HashPassword(
                newPassword);


        _userRepository.Update(
            customer);


        return null;
    }


    // Opretter et sikkert token til nulstilling af password.
    // Det oprindelige token skal senere sendes til kunden.
    public string? RequestPasswordReset(
        string email)
    {
        string normalizedEmail =
            email
                .Trim()
                .ToLowerInvariant();


        User? user =
            _userRepository.GetByEmail(
                normalizedEmail);


        // Kun aktive kunder må bruge customer password reset.
        // Admin bruger ikke dette flow.
        if (user is not Customer ||
            !user.IsActive)
        {
            return null;
        }


        // Fjerner gamle eller allerede anvendte tokens
        _passwordResetTokenRepository
            .DeleteExpiredTokens();


        // Opretter et kryptografisk sikkert token
        string token =
            Convert.ToHexString(
                RandomNumberGenerator.GetBytes(
                    32));


        // Kun tokenets hash gemmes i databasen
        string tokenHash =
            HashResetToken(
                token);


        PasswordResetToken passwordResetToken =
            new PasswordResetToken
            {
                UserId = user.Id,

                TokenHash = tokenHash,

                // Tokenet kan bruges i 30 minutter
                ExpiresAt =
                    DateTime.UtcNow.AddMinutes(
                        30),

                // Null betyder, at tokenet ikke er brugt endnu
                UsedAt = null
            };


        _passwordResetTokenRepository.Add(
            passwordResetToken);


        // Det oprindelige token gemmes aldrig i databasen.
        // Det skal bruges i reset-linket til kunden.
        return token;
    }


    // Nulstiller kundens password ved hjælp af et gyldigt token.
    // Null betyder, at passwordet blev nulstillet.
    public string? ResetPassword(
        string token,
        string newPassword)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return "The password reset link is invalid or has expired.";
        }


        string tokenHash =
            HashResetToken(
                token);


        PasswordResetToken? passwordResetToken =
            _passwordResetTokenRepository
                .GetByTokenHash(
                    tokenHash);


        // Tokenet skal eksistere, ikke være udløbet
        // og må ikke tidligere være blevet brugt.
        if (passwordResetToken == null ||
            !passwordResetToken.IsValid())
        {
            return "The password reset link is invalid or has expired.";
        }


        User? user =
            _userRepository.GetById(
                passwordResetToken.UserId);


        // Kun aktive kunder må bruge dette reset-flow
        if (user is not Customer customer ||
            !customer.IsActive)
        {
            return "The password reset link is invalid or has expired.";
        }


        string? passwordError =
            PasswordValidator.Validate(
                newPassword,
                customer.Name,
                customer.Email);


        if (passwordError != null)
        {
            return passwordError;
        }


        // Det nye password må ikke være det samme som det gamle
        bool passwordIsUnchanged =
            _passwordHasher.VerifyPassword(
                newPassword,
                customer.PasswordHash);


        if (passwordIsUnchanged)
        {
            return "The new password must be different from the previous password.";
        }


        // Gemmer kun det nye password som en hash
        customer.PasswordHash =
            _passwordHasher.HashPassword(
                newPassword);


        _userRepository.Update(
            customer);


        // Markerer tokenet som brugt,
        // så det ikke kan genbruges
        passwordResetToken.UsedAt =
            DateTime.UtcNow;


        _passwordResetTokenRepository.Update(
            passwordResetToken);


        return null;
    }


    // Laver en SHA-256-hash af reset-tokenet
    private static string HashResetToken(
        string token)
    {
        byte[] tokenBytes =
            Encoding.UTF8.GetBytes(
                token);


        byte[] tokenHash =
            SHA256.HashData(
                tokenBytes);


        return Convert.ToHexString(
            tokenHash);
    }


    // Registrerer et fejlet loginforsøg
    private void RegisterFailedLogin(
        string email)
    {
        string cacheKey =
            GetLoginAttemptKey(
                email);


        LoginAttempt loginAttempt =
            _memoryCache
                .Get<LoginAttempt>(
                    cacheKey)
            ?? new LoginAttempt();


        loginAttempt.NumberOfAttempts++;


        // Logger det fejlede loginforsøg
        _logger.LogWarning(
            "Failed login attempt. Attempt number: {AttemptNumber}",
            loginAttempt.NumberOfAttempts);


        // Kontoens login begrænses midlertidigt efter fem forsøg
        if (loginAttempt.NumberOfAttempts >=
            MaximumLoginAttempts)
        {
            loginAttempt.BlockedUntil =
                DateTime.UtcNow
                    .AddMinutes(
                        15);


            // Logger når login bliver midlertidigt blokeret
            _logger.LogWarning(
                "Login temporarily blocked after repeated failed attempts.");
        }


        // Oplysningerne fjernes automatisk efter 15 minutter
        _memoryCache.Set(
            cacheKey,
            loginAttempt,
            TimeSpan.FromMinutes(
                15));
    }


    // Kontrollerer om login er midlertidigt blokeret
    private bool IsLoginBlocked(
        string email)
    {
        LoginAttempt? loginAttempt =
            _memoryCache.Get<LoginAttempt>(
                GetLoginAttemptKey(
                    email));


        if (loginAttempt?.BlockedUntil == null)
        {
            return false;
        }


        return loginAttempt.BlockedUntil >
               DateTime.UtcNow;
    }


    // Laver en unik nøgle til fejlede loginforsøg
    private static string GetLoginAttemptKey(
        string email)
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
