using Obligatoris.opgave.Domain.Models;
namespace Obligatoris.opgave.Domain.Interfaces;

// Interfacet beskriver de databasefunktioner,
// der skal bruges til kundens indkøbskurv.
public interface IShoppingCartRepository
{
    // Finder kundens indkøbskurv
    ShoppingCart? GetByCustomerId(int customerId);

    // Opretter en ny indkøbskurv og returnerer kurvens id
    int Create(int customerId);

    // Henter produkterne fra en bestemt indkøbskurv
    List<Product> GetProducts(int shoppingCartId);

    // Henter antallet af et bestemt produkt i kurven
    int GetProductQuantity(int shoppingCartId, int productId);

    // Tilføjer et produkt til kurven
    void AddProduct(int shoppingCartId, int productId, int quantity);

    // Ændrer antallet af et produkt
    void UpdateQuantity(int shoppingCartId, int productId, int quantity);

    // Fjerner ét produkt fra kurven
    void RemoveProduct(int shoppingCartId, int productId);

    // Fjerner alle produkter fra kurven
    void Clear(int shoppingCartId);
}