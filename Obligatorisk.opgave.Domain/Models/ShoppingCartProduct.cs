namespace Obligatoris.opgave.Domain.Models;

//repræsenterer en indkøbskurv, som tilhører en kunde og gemmes i db

// Repræsenterer et produkt og dets antal i en indkøbskurv
public class ShoppingCartProduct
{
    public int ProductId { get; set; }
    
    public int Quantity { get; set; }
}