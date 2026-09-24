using Obligatoris.opgave.Domain.Models;

namespace Obligatoris.opgave.Domain.Interfaces;

// Interfacet beskriver de databasefunktioner,
// der skal bruges til kundernes ordrer.
public interface IOrderRepository
{
    // Henter én ordre ud fra ordrens id
    Order? GetById(int id);

    // Henter alle ordrer, der tilhører en bestemt kunde
    List<Order> GetByCustomerId(int customerId);

    // Gemmer en ny ordre og returnerer det nye ordre-id
    int Add(Order order);

    // Forbinder et produkt med en ordre
    void AddProductToOrder(
        int orderId,
        int productId,
        int quantity,
        decimal priceAtPurchase);

    // Opdaterer ordrens status
    void UpdateStatus(int orderId, OrderStatus status);
}