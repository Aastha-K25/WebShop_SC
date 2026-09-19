namespace Obligatoris.opgave.Domain.Models;

public class ShoppingCart
{
    public int Id { get; set; }
    public int CustomerId { get; set; }

    //den beregner totalprisen inkluderet levering. 
    public decimal CalculateTotal(decimal productsPrice, decimal shippingPrice)
    {
        return productsPrice + shippingPrice;
    }
}