
using Obligatoris.opgave.Domain.Interfaces;
using Obligatoris.opgave.Domain.Models;
using Obligatorisk_opgave.Security;
using Obligatorisk_opgave.Services;
using Microsoft.AspNetCore.Http;
using Serilog;
using Serilog.Events;
using Obligatorisk_opgave.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);


// ==================================================
// SECURITY LOGGING
// ==================================================

// Gemmer logfiler lokalt uden for wwwroot
string logFolder =
    Path.Combine(
        Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData),
        "PaperAndPetals",
        "Logs");


// Opretter mappen hvis den ikke findes
Directory.CreateDirectory(logFolder);


// Bestemmer hvor logfilerne skal gemmes
string logPath =
    Path.Combine(
        logFolder,
        "security-.log");


// Gemmer logmeddelelser i filer
// Opretter en ny logfil hver dag
Log.Logger =
    new LoggerConfiguration()
        .MinimumLevel.Information()
        .MinimumLevel.Override(
            "Microsoft",
            LogEventLevel.Warning)
        .WriteTo.Console()
        .WriteTo.File(
            logPath,
            rollingInterval: RollingInterval.Day,
            fileSizeLimitBytes: 5_000_000,
            rollOnFileSizeLimit: true,
            retainedFileCountLimit: 7)
        .CreateLogger();


// Gør Serilog tilgængelig gennem ILogger
builder.Host.UseSerilog();


// Gør Razor Pages klar
builder.Services.AddRazorPages();

builder.Services.AddHttpContextAccessor();


// Bruges til midlertidigt at registrere fejlede loginforsøg
builder.Services.AddMemoryCache();


// Gemmer sessionsdata midlertidigt i serverens hukommelse
builder.Services.AddDistributedMemoryCache();


builder.Services.AddSession(options =>
{
    options.Cookie.Name =
        "PaperAndPetals.Session";

    options.Cookie.HttpOnly =
        true;

    options.Cookie.IsEssential =
        true;

    options.Cookie.SecurePolicy =
        CookieSecurePolicy.Always;

    options.Cookie.SameSite =
        SameSiteMode.Strict;

    options.IdleTimeout =
        TimeSpan.FromMinutes(30);
});


// ==================================================
// DEPENDENCY INJECTION
// ==================================================

builder.Services.AddScoped<IProductRepository, ProductRepository>();

builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddScoped<
    IPasswordResetTokenRepository,
    PasswordResetTokenRepository>();

builder.Services.AddScoped<IOrderRepository, OrderRepository>();

builder.Services.AddScoped<
    IShoppingCartRepository,
    ShoppingCartRepository>();


// Bruges til sikker hashing og kontrol af passwords
builder.Services.AddScoped<PasswordHasher>();


// Håndterer loginoplysninger i ASP.NET Session
builder.Services.AddScoped<SessionService>();


// Samler registration, login og logout ét sted
builder.Services.AddScoped<AuthenticationService>();


// Håndterer roller, adgangskontrol og ownership
builder.Services.AddScoped<AuthorizationService>();


var app = builder.Build();


// ==================================================
// ADMIN SEED
// ==================================================

// Admin oprettes kun automatisk i Development.
// Passwordet hentes fra User Secrets og ligger derfor
// ikke hardcoded i kildekoden.
if (app.Environment.IsDevelopment())
{
    using IServiceScope scope =
        app.Services.CreateScope();


    IConfiguration configuration =
        scope.ServiceProvider
            .GetRequiredService<IConfiguration>();


    IUserRepository userRepository =
        scope.ServiceProvider
            .GetRequiredService<IUserRepository>();


    PasswordHasher passwordHasher =
        scope.ServiceProvider
            .GetRequiredService<PasswordHasher>();


    string? name =
        configuration["AdminSeed:Name"];

    string? surname =
        configuration["AdminSeed:Surname"];

    string? email =
        configuration["AdminSeed:Email"];

    string? password =
        configuration["AdminSeed:Password"];

    string? employeeNumber =
        configuration["AdminSeed:EmployeeNumber"];


    // Seeder kører kun, hvis alle nødvendige
    // admin-oplysninger findes i User Secrets.
    if (!string.IsNullOrWhiteSpace(name) &&
        !string.IsNullOrWhiteSpace(surname) &&
        !string.IsNullOrWhiteSpace(email) &&
        !string.IsNullOrWhiteSpace(password) &&
        !string.IsNullOrWhiteSpace(employeeNumber))
    {
        string normalizedEmail =
            email.Trim().ToLowerInvariant();


        // Kontrollerer om admin-brugeren allerede findes.
        User? existingUser =
            userRepository.GetByEmail(
                normalizedEmail);


        // Admin oprettes kun én gang.
        if (existingUser == null)
        {
            // Admin-passwordet skal følge samme
            // password policy som resten af systemet.
            string? passwordError =
                PasswordValidator.Validate(
                    password,
                    name,
                    normalizedEmail);


            if (passwordError != null)
            {
                throw new InvalidOperationException(
                    "Admin password does not satisfy the password policy: "
                    + passwordError);
            }


            // Passwordet hashes før Admin-objektet
            // sendes videre til repository.
            string passwordHash =
                passwordHasher.HashPassword(
                    password);


            Admin admin =
                new Admin
                {
                    Name =
                        name.Trim(),

                    Surname =
                        surname.Trim(),

                    Email =
                        normalizedEmail,

                    PasswordHash =
                        passwordHash,

                    IsActive =
                        true,

                    EmployeeNumber =
                        employeeNumber.Trim(),

                    // MFA er forberedt i modellen,
                    // men selve MFA-flowet er ikke implementeret endnu.
                    MfaEnabled =
                        false
                };


            // Repository gemmer Admin i både
            // Users og Admins i samme transaction.
            userRepository.Add(
                admin);
        }
    }
}


// ==================================================
// ERROR HANDLING
// ==================================================

// Hvis vi ikke kører i development
if (!app.Environment.IsDevelopment())
{
    // Viser vores error side ved fejl
    app.UseExceptionHandler("/Error");


    // Tvinger browseren til at bruge HTTPS fremover
    app.UseHsts();
}


// Sender HTTP videre til HTTPS
app.UseHttpsRedirection();



// ==================================================
// ALLOWED HTTP METHODS
// ==================================================

// Tillader kun de HTTP metoder vi bruger
app.Use(async (context, next) =>
{
    HashSet<string> allowedMethods =
        new HashSet<string>
        {
            "GET",
            "POST",
            "OPTIONS"
        };


    // Blokerer andre HTTP-metoder
    if (!allowedMethods.Contains(
            context.Request.Method))
    {
        context.Response.StatusCode =
            405;


        await context.Response.WriteAsync(
            "Method Not Allowed");


        return;
    }


    await next();
});



// ==================================================
// CONTENT TYPE VALIDATION
// ==================================================

// Tjekker Content-Type på POST requests
app.Use(async (context, next) =>
{
    // Vi tjekker kun POST fordi GET
    // ikke sender form-data i body
    if (context.Request.Method == "POST")
    {
        string? contentType =
            context.Request.ContentType;


        // Vi tillader kun de formater vores forms bruger
        bool validContentType =
            contentType != null &&
            (
                contentType.StartsWith(
                    "application/x-www-form-urlencoded")
                ||
                contentType.StartsWith(
                    "multipart/form-data")
            );


        // Stopper requesten hvis Content-Type
        // ikke er tilladt
        if (!validContentType)
        {
            context.Response.StatusCode =
                415;


            await context.Response.WriteAsync(
                "Unsupported Media Type");


            return;
        }
    }


    await next();
});



// ==================================================
// SECURITY HEADERS
// ==================================================

// Tilføjer sikkerhedsheaders
app.Use(async (context, next) =>
{
    // Stopper browseren fra selv at gætte MIME-type
    context.Response.Headers.Append(
        "X-Content-Type-Options",
        "nosniff");


    // CSP bestemmer hvilke ressourcer browseren må bruge
    context.Response.Headers.Append(
        "Content-Security-Policy",
        "default-src 'self'; " +
        "script-src 'self'; " +
        "style-src 'self'; " +
        "img-src 'self'; " +
        "object-src 'none'; " +
        "frame-ancestors 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'; " +
        "upgrade-insecure-requests;");


    await next();
});



// ==================================================
// ROUTING OG SESSION
// ==================================================

// Gør routing klar
app.UseRouting();


// Gør Session tilgængelig
app.UseSession();


// Gør authorization klar
app.UseAuthorization();


// Gør statiske filer klar
app.MapStaticAssets();


// Finder Razor Pages
app.MapRazorPages()
    .WithStaticAssets();


// Starter programmet
app.Run();
