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

    // Henter alle ordrer
    List<Order> GetAll();

    // Henter de produkter, der tilhører en bestemt ordre
    List<OrderProduct> GetOrderProducts(int orderId);

    // Gemmer en ny ordre og returnerer det nye ordre-id
    int Add(Order order);

    // Forbinder et produkt med en ordre
    void AddProductToOrder(
        int orderId,
        int productId,
        int quantity,
        decimal priceAtPurchase);

    // Gennemfører checkout i én samlet transaction
    // Returnerer id på den nye ordre
    int PlaceOrder(
        int customerId,
        decimal shippingPrice);

    // Opdaterer ordrens status
    void UpdateStatus(
        int orderId,
        OrderStatus status);

}