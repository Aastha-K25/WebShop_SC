
using System.Text; // Bruges til at lave tekst om til bytes
using Microsoft.Extensions.Caching.Memory; // Bruges til at gemme loginforsøg midlertidigt
using Obligatoris.opgave.Domain.Models; // Giver adgang til vores modeller
using Obligatoris.opgave.Domain.Interfaces; // Giver adgang til vores interfaces
using Obligatorisk_opgave.Security; // Giver adgang til PasswordHasher og PasswordValidator
using System.Security.Cryptography; // Bruges til sikre tokens og SHA256

namespace Obligatorisk_opgave.Services;

// Service som håndterer login, registrering og passwords
public class AuthenticationService
{
    // Bruges til at hente og gemme brugere i databasen
    private readonly IUserRepository _userRepository;

    // Bruges til at hashe og kontrollere passwords
    private readonly PasswordHasher _passwordHasher;

    // Bruges til at oprette og afslutte brugerens session
    private readonly SessionService _sessionService;

    // Gemmer oplysninger om fejlede loginforsøg midlertidigt
    private readonly IMemoryCache _memoryCache;

    // Bruges til at logge sikkerhedshændelser
    private readonly ILogger<AuthenticationService> _logger;

    // Bruges til at gemme og hente password reset tokens
    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;

    // Bestemmer at login blokeres efter fem fejlede forsøg
    private const int MaximumLoginAttempts = 5;

    // Constructor som modtager vores repositories og services
    public AuthenticationService(
        IUserRepository userRepository,
        IPasswordResetTokenRepository passwordResetTokenRepository,
        PasswordHasher passwordHasher,
        SessionService sessionService,
        IMemoryCache memoryCache,
        ILogger<AuthenticationService> logger)
    {
        // Gemmer vores user repository
        _userRepository = userRepository;

        // Gemmer vores password reset repository
        _passwordResetTokenRepository = passwordResetTokenRepository;

        // Gemmer vores password hasher
        _passwordHasher = passwordHasher;

        // Gemmer vores session service
        _sessionService = sessionService;

        // Gemmer vores memory cache
        _memoryCache = memoryCache;

        // Gemmer vores logger
        _logger = logger;
    }

    // Metode som opretter en ny kundekonto
    // Returnerer null hvis registreringen lykkes
    public string? RegisterCustomer(
        string name,
        string surname,
        string email,
        string password,
        string address)
    {
        // Fjerner ekstra mellemrum fra navnet
        name = name.Trim();

        // Fjerner ekstra mellemrum fra efternavnet
        surname = surname.Trim();

        // Fjerner mellemrum og laver email om til små bogstaver
        email = email.Trim().ToLowerInvariant();

        // Fjerner ekstra mellemrum fra adressen
        address = address.Trim();

        // Kontrollerer om et af felterne er tomme
        if (string.IsNullOrWhiteSpace(name) ||
            string.IsNullOrWhiteSpace(surname) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(address))
        {
            // Stopper hvis der mangler oplysninger
            return "Please fill out all fields.";
        }

        // Kontrollerer om email allerede findes i databasen
        if (_userRepository.EmailExists(email))
        {
            // Afviser registreringen hvis email allerede bruges
            return "An account with this email already exists.";
        }

        // Kontrollerer passwordet ud fra vores password policy
        string? passwordError = PasswordValidator.Validate(password, name, email);

        // Tjekker om passwordet ikke opfylder reglerne
        if (passwordError != null)
        {
            // Returnerer fejlbeskeden fra PasswordValidator
            return passwordError;
        }

        // Hasher passwordet så det ikke gemmes i klartekst
        string passwordHash = _passwordHasher.HashPassword(password);

        // Opretter et nyt Customer objekt
        Customer customer = new Customer
        {
            // Gemmer kundens navn
            Name = name,

            // Gemmer kundens efternavn
            Surname = surname,

            // Gemmer kundens email
            Email = email,

            // Gemmer kundens adresse
            Address = address,

            // Gemmer passwordets hash
            PasswordHash = passwordHash,

            // Gør den nye kundekonto aktiv
            IsActive = true
        };

        // Gemmer den nye kunde i databasen
        _userRepository.Add(customer);

        // Returnerer null fordi registreringen lykkedes
        return null;
    }

    // Metode som logger en bruger ind
    // Returnerer null hvis login lykkes
    public string? Login(string email, string password)
    {
        // Fjerner mellemrum og laver email om til små bogstaver
        string normalizedEmail = email.Trim().ToLowerInvariant();

        // Kontrollerer om email eller password mangler
        if (string.IsNullOrWhiteSpace(normalizedEmail) ||
            string.IsNullOrWhiteSpace(password))
        {
            // Stopper hvis loginoplysningerne mangler
            return "Please enter email and password.";
        }

        // Kontrollerer om login er midlertidigt blokeret
        if (IsLoginBlocked(normalizedEmail))
        {
            // Skriver en advarsel i vores sikkerhedslog
            _logger.LogWarning("Login attempt blocked due to too many failed attempts.");

            // Fortæller brugeren at der har været for mange forsøg
            return "Too many failed login attempts. Please try again later.";
        }

        // Finder brugeren i databasen ud fra email
        User? user = _userRepository.GetByEmail(normalizedEmail);

        // Kontrollerer om brugeren findes og er aktiv
        if (user == null || !user.IsActive)
        {
            // Registrerer et fejlet loginforsøg
            RegisterFailedLogin(normalizedEmail);

            // Viser samme besked ved ukendt email og inaktiv konto
            return "Invalid email or password.";
        }

        // Kontrollerer om passwordet passer til den gemte hash
        bool passwordIsCorrect = _passwordHasher.VerifyPassword(
            password, user.PasswordHash);

        // Tjekker om passwordet er forkert
        if (!passwordIsCorrect)
        {
            // Registrerer et fejlet loginforsøg
            RegisterFailedLogin(normalizedEmail);

            // Bruger samme fejlbesked så vi ikke afslører kontooplysninger
            return "Invalid email or password.";
        }

        // Kontrollerer om brugeren er Admin med MFA aktiveret
        if (user is Admin admin && admin.MfaEnabled)
        {
            // Afviser login fordi MFA endnu ikke er implementeret
            return "MFA is required for this account, but the MFA flow is not available yet.";
        }

        // Fjerner tidligere fejlede loginforsøg fra memory cache
        _memoryCache.Remove(GetLoginAttemptKey(normalizedEmail));

        // Opretter en session så brugeren bliver logget ind
        _sessionService.CreateSession(user);

        // Logger et vellykket login med brugerens id
        // Passwordet bliver ikke skrevet i loggen
        _logger.LogInformation(
            "User successfully authenticated. UserId: {UserId}",
            user.Id);

        // Returnerer null fordi login lykkedes
        return null;
    }

    // Metode som henter brugeren der er logget ind
    public User? GetCurrentUser()
    {
        // Henter den aktuelle bruger fra sessionen
        return _sessionService.GetCurrentUser();
    }

    // Metode som kontrollerer om en bruger er logget ind
    public bool IsAuthenticated()
    {
        // Returnerer true hvis der er en aktiv brugersession
        return _sessionService.IsLoggedIn();
    }

    // Metode som logger brugeren ud
    public void Logout()
    {
        // Fjerner brugerens login fra sessionen
        _sessionService.Logout();
    }

    // Metode som ændrer password for en kunde der er logget ind
    // Returnerer null hvis ændringen lykkes
    public string? ChangePassword(string currentPassword, string newPassword)
    {
        // Henter brugeren som er logget ind
        User? currentUser = _sessionService.GetCurrentUser();

        // Kontrollerer om brugeren er en Customer
        if (currentUser is not Customer customer)
        {
            // Stopper hvis brugeren ikke er logget ind som kunde
            return "You must be logged in as a customer.";
        }

        // Kontrollerer om det nuværende password er korrekt
        bool currentPasswordIsCorrect = _passwordHasher.VerifyPassword(
            currentPassword, customer.PasswordHash);

        // Tjekker om det nuværende password er forkert
        if (!currentPasswordIsCorrect)
        {
            // Afviser ændringen hvis passwordet ikke passer
            return "The current password is incorrect.";
        }

        // Kontrollerer om det nye password er det samme som det gamle
        bool passwordIsUnchanged = _passwordHasher.VerifyPassword(
            newPassword, customer.PasswordHash);

        // Tjekker om kunden forsøger at genbruge det gamle password
        if (passwordIsUnchanged)
        {
            // Afviser ændringen hvis passwordet ikke er nyt
            return "The new password must be different from the current password.";
        }

        // Kontrollerer det nye password ud fra vores password policy
        string? passwordError = PasswordValidator.Validate(
            newPassword, customer.Name, customer.Email);

        // Tjekker om det nye password overholder reglerne
        if (passwordError != null)
        {
            // Returnerer fejlbeskeden fra PasswordValidator
            return passwordError;
        }

        // Hasher det nye password og gemmer hashen på kunden
        customer.PasswordHash = _passwordHasher.HashPassword(newPassword);

        // Gemmer kundens nye password hash i databasen
        _userRepository.Update(customer);

        // Returnerer null fordi passwordet blev ændret
        return null;
    }

    // Metode som opretter et token til nulstilling af password
    // Returnerer null hvis brugeren ikke er en aktiv kunde
    public string? RequestPasswordReset(string email)
    {
        // Fjerner mellemrum og laver email om til små bogstaver
        string normalizedEmail = email.Trim().ToLowerInvariant();

        // Finder brugeren i databasen ud fra email
        User? user = _userRepository.GetByEmail(normalizedEmail);

        // Kontrollerer om brugeren er en aktiv Customer
        if (user is not Customer || !user.IsActive)
        {
            // Returnerer null hvis brugeren ikke må bruge password reset
            return null;
        }

        // Sletter tokens som er udløbet eller allerede brugt
        _passwordResetTokenRepository.DeleteExpiredTokens();

        // Opretter 32 tilfældige bytes med en sikker generator
        // Convert.ToHexString laver dem om til et token som tekst
        string token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

        // Laver en hash af tokenet før det gemmes
        string tokenHash = HashResetToken(token);

        // Opretter et nyt PasswordResetToken objekt
        PasswordResetToken passwordResetToken = new PasswordResetToken
        {
            // Gemmer id på kunden som skal nulstille password
            UserId = user.Id,

            // Gemmer kun tokenets hash
            TokenHash = tokenHash,

            // Tokenet udløber 30 minutter efter oprettelsen
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),

            // Null betyder at tokenet ikke er blevet brugt
            UsedAt = null
        };

        // Gemmer tokenets hash og oplysninger i databasen
        _passwordResetTokenRepository.Add(passwordResetToken);

        // Returnerer det oprindelige token
        // Det skal senere kunne bruges i et reset link
        return token;
    }

    // Metode som nulstiller password ved hjælp af et token
    // Returnerer null hvis nulstillingen lykkes
    public string? ResetPassword(string token, string newPassword)
    {
        // Kontrollerer om tokenet mangler
        if (string.IsNullOrWhiteSpace(token))
        {
            // Afviser hvis der ikke er et token
            return "The password reset link is invalid or has expired.";
        }

        // Laver en hash af tokenet så det kan findes i databasen
        string tokenHash = HashResetToken(token);

        // Finder det gemte token ud fra dets hash
        PasswordResetToken? passwordResetToken =
            _passwordResetTokenRepository.GetByTokenHash(tokenHash);

        // Kontrollerer om tokenet findes og stadig er gyldigt
        // IsValid tjekker reglerne for tokenets gyldighed
        if (passwordResetToken == null || !passwordResetToken.IsValid())
        {
            // Afviser hvis tokenet er ugyldigt eller udløbet
            return "The password reset link is invalid or has expired.";
        }

        // Finder brugeren som tokenet tilhører
        User? user = _userRepository.GetById(passwordResetToken.UserId);

        // Kontrollerer om brugeren er en aktiv Customer
        if (user is not Customer customer || !customer.IsActive)
        {
            // Afviser nulstillingen hvis kunden ikke er aktiv
            return "The password reset link is invalid or has expired.";
        }

        // Kontrollerer det nye password ud fra vores password policy
        string? passwordError = PasswordValidator.Validate(
            newPassword, customer.Name, customer.Email);

        // Tjekker om det nye password overholder reglerne
        if (passwordError != null)
        {
            // Returnerer fejlbeskeden hvis passwordet er ugyldigt
            return passwordError;
        }

        // Kontrollerer om det nye password er det samme som det gamle
        bool passwordIsUnchanged = _passwordHasher.VerifyPassword(
            newPassword, customer.PasswordHash);

        // Tjekker om kunden genbruger sit tidligere password
        if (passwordIsUnchanged)
        {
            // Afviser hvis passwordet ikke er ændret
            return "The new password must be different from the previous password.";
        }

        // Hasher det nye password og gemmer hashen på kunden
        customer.PasswordHash = _passwordHasher.HashPassword(newPassword);

        // Opdaterer kundens password hash i databasen
        _userRepository.Update(customer);

        // Gemmer tidspunktet hvor tokenet blev brugt
        passwordResetToken.UsedAt = DateTime.UtcNow;

        // Opdaterer tokenet så det er markeret som brugt
        _passwordResetTokenRepository.Update(passwordResetToken);

        // Returnerer null fordi passwordet blev nulstillet
        return null;
    }

    // Hjælpemetode som laver en SHA256 hash af et reset token
    private static string HashResetToken(string token)
    {
        // Laver tokenets tekst om til bytes
        byte[] tokenBytes = Encoding.UTF8.GetBytes(token);

        // Hasher tokenets bytes med SHA256
        byte[] tokenHash = SHA256.HashData(tokenBytes);

        // Laver hashen om til tekst så den kan gemmes i databasen
        return Convert.ToHexString(tokenHash);
    }

    // Hjælpemetode som registrerer et fejlet loginforsøg
    private void RegisterFailedLogin(string email)
    {
        // Laver en nøgle som bruges til at gemme loginforsøg
        string cacheKey = GetLoginAttemptKey(email);

        // Henter tidligere loginforsøg fra memory cache
        // Opretter et nyt LoginAttempt objekt hvis der ikke findes et
        LoginAttempt loginAttempt = _memoryCache.Get<LoginAttempt>(cacheKey)
            ?? new LoginAttempt();

        // Lægger én til antallet af fejlede loginforsøg
        loginAttempt.NumberOfAttempts++;

        // Skriver en advarsel i sikkerhedsloggen
        // Gemmer forsøgsnummeret uden at logge password
        _logger.LogWarning(
            "Failed login attempt. Attempt number: {AttemptNumber}",
            loginAttempt.NumberOfAttempts);

        // Kontrollerer om grænsen på fem fejlede forsøg er nået
        if (loginAttempt.NumberOfAttempts >= MaximumLoginAttempts)
        {
            // Blokerer login indtil 15 minutter fra nu
            loginAttempt.BlockedUntil = DateTime.UtcNow.AddMinutes(15);

            // Skriver en advarsel om blokeringen i sikkerhedsloggen
            _logger.LogWarning(
                "Login temporarily blocked after repeated failed attempts.");
        }

        // Gemmer loginforsøgene i memory cache i 15 minutter
        _memoryCache.Set(cacheKey, loginAttempt, TimeSpan.FromMinutes(15));
    }

    // Hjælpemetode som kontrollerer om login er blokeret
    private bool IsLoginBlocked(string email)
    {
        // Henter oplysninger om fejlede forsøg for denne email
        LoginAttempt? loginAttempt = _memoryCache.Get<LoginAttempt>(
            GetLoginAttemptKey(email));

        // Kontrollerer om der findes en blokeringstid
        if (loginAttempt?.BlockedUntil == null)
        {
            // Returnerer false hvis login ikke er blokeret
            return false;
        }

        // Returnerer true hvis blokeringstiden stadig ligger i fremtiden
        return loginAttempt.BlockedUntil > DateTime.UtcNow;
    }

    // Hjælpemetode som laver en nøgle til loginforsøg
    private static string GetLoginAttemptKey(string email)
    {
        // Bruger email til at kende forskel på loginforsøg
        return $"LoginAttempt:{email}";
    }

    // Lille klasse som holder styr på fejlede loginforsøg
    private class LoginAttempt
    {
        // Gemmer antallet af fejlede loginforsøg
        public int NumberOfAttempts { get; set; }

        // Gemmer tidspunktet hvor blokeringen udløber
        // Spørgsmålstegnet betyder at værdien må være null
        public DateTime? BlockedUntil { get; set; }
    }
}
