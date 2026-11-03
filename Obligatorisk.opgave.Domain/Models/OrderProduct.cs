namespace Obligatoris.opgave.Domain.Models;

public class OrderProduct
{
    public int OrderId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public decimal PriceAtPurchase { get; set; }
}
