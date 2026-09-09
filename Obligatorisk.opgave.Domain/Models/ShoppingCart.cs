namespace Obligatoris.opgave.Domain.Models;

public class ShoppingCart
{
    public int Id { get; set; }
    public int CustomerId { get; set; }

    //den siger pris * antal 
    public decimal CalculateTotal(IEnumerable<(decimal Price, int Quantity)> products)
    {
        return products.Sum(product => product.Price * product.Quantity);
    }
}