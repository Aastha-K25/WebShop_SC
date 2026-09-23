namespace Obligatoris.opgave.Domain.Models;

public class Order
{
    public int Id { get; set; }

    // Viser hvilken kunde ordren tilhører
    public int CustomerId { get; set; }

    public DateTime OrderDate { get; set; }

    public decimal TotalPrice { get; set; }

    public OrderStatus Status { get; set; }
}