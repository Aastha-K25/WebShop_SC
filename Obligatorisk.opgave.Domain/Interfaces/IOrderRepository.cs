
using Obligatoris.opgave.Domain.Models; // Giver adgang til vores modeller

namespace Obligatoris.opgave.Domain.Interfaces;

// Interface som bestemmer hvilke metoder OrderRepository skal have
// Interfacet beskriver hvad der skal kunne gøres med ordrer
// Selve koden til metoderne ligger i OrderRepository
public interface IOrderRepository
{
    // Henter en bestemt ordre fra databasen ud fra dens id
    // Spørgsmålstegnet betyder at resultatet må være null
    Order? GetById(int id);

    // Henter alle ordrer som tilhører en bestemt kunde
    // customerId bruges til at finde kundens ordrer
    List<Order> GetByCustomerId(int customerId);

    // Henter alle ordrer fra databasen
    // Returnerer en liste med Order objekter
    List<Order> GetAll();

    // Henter produkterne som tilhører en bestemt ordre
    // orderId fortæller hvilken ordre vi vil hente produkter fra
    List<OrderProduct> GetOrderProducts(int orderId);

    // Gemmer en ny ordre i databasen
    // Modtager et Order objekt og returnerer det nye ordre id
    int Add(Order order);

    // Tilføjer et produkt til en bestemt ordre
    // orderId er den ordre produktet skal tilhøre
    // productId er produktets id
    // quantity er antallet af produktet
    // priceAtPurchase er produktets pris på købstidspunktet
    void AddProductToOrder(
        int orderId,
        int productId,
        int quantity,
        decimal priceAtPurchase);

    // Gennemfører checkout i én samlet database transaction
    // customerId fortæller hvilken kunde der bestiller
    // shippingPrice er prisen på levering
    // Returnerer id på den nye ordre
    int PlaceOrder(int customerId, decimal shippingPrice);

    // Opdaterer status på en bestemt ordre
    // orderId bruges til at finde ordren
    // status er den nye ordrestatus
    void UpdateStatus(int orderId, OrderStatus status);
}
