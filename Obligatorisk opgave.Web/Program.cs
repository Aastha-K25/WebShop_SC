using Obligatoris.opgave.Domain.Interfaces;
using Obligatorisk_opgave.Security;
using Obligatorisk_opgave.Services;
using Microsoft.AspNetCore.Http;
using Obligatorisk_opgave.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);


// Gør Razor Pages klar
builder.Services.AddRazorPages();

builder.Services.AddHttpContextAccessor();

builder.Services.AddMemoryCache(); //Bruges til midlertidigt at registere fejlede loginforsøg

builder.Services.AddDistributedMemoryCache(); //Gemmer sessionsdata midlertidigt i servens hukommelse

builder.Services.AddSession(options =>
{
    options.Cookie.Name = "PaperAndPetales.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

// Dependency Injection
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IShoppingCartRepository, ShoppingCartRepository>();
builder.Services.AddScoped<PasswordHasher>(); //Bruges til sikre hashin og kontorl af passwords
builder.Services.AddScoped<SessionService>(); //håndtere loginoplysninger i ASP.NET session
builder.Services.AddScoped<AuthenticationService>(); //Samler registration login og loyout et sted

var app = builder.Build();


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



// Tillader kun de HTTP metoder vi bruger
app.Use(async (context, next) =>
{
    HashSet<string> allowedMethods = new HashSet<string>
        {
            "GET",
            "POST",
            "OPTIONS"
        };


    // Blokerer andre HTTP-metoder
    if (!allowedMethods.Contains(context.Request.Method))
    {
        context.Response.StatusCode = 405;


        await context.Response.WriteAsync( "Method Not Allowed");


        return;
    }


    await next();
});



// Tjekker Content-Type på POST requests
app.Use(async (context, next) =>
{
    // Vi tjekker kun POST fordi GET ikke sender form-data i body
    if (context.Request.Method == "POST")
    {
        string? contentType =
            context.Request.ContentType;


        // Vi tillader kun de formater vores forms bruger
        bool validContentType =
            contentType != null &&
            (
                contentType.StartsWith( "application/x-www-form-urlencoded") ||  contentType.StartsWith( "multipart/form-data")
            );


        // Stopper requesten hvis Content-Type ikke er tilladt
        if (!validContentType)
        {
            context.Response.StatusCode = 415;


            await context.Response.WriteAsync( "Unsupported Media Type");


            return;
        }
    }


    await next();
});



// Tilføjer sikkerhedsheaders
app.Use(async (context, next) =>
{
    // Stopper browseren fra selv at g�tte MIME-type
    context.Response.Headers.Append(
        "X-Content-Type-Options",
        "nosniff");


    // CSP bestemmer hvilke ressourcer browseren m� bruge
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



// Gør routing klar
app.UseRouting();

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