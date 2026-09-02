using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Obligatorisk_opgave.Pages;

public class Cart : PageModel
{
    // Midlertidige produktdata til kurven
    public string ProductName { get; set; } = "Romantisk buket";

    public decimal Price { get; set; } = 449;

    public int Quantity { get; set; } = 1;

    // Fast leveringspris indtil videre
    public decimal ShippingPrice { get; set; } = 49;

    // Beregner prisen ud fra pris og antal
    public decimal Subtotal => Price * Quantity;

    // Beregner den samlede pris inkl. levering
    public decimal TotalPrice => Subtotal + ShippingPrice;

    // Kører når Cart-siden åbnes med en GET request
    public void OnGet()
    {
    }
}