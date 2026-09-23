using Obligatoris.opgave.Domain.Interfaces;
using System;
using Obligatoris.opgave.Domain.Models;

namespace Obligatorisk_opgave.Infrastructure.Repositories;


// ShoppingCartRepository får ansvaret for kommunikationen
// mellem indkøbskurven og databasen.
public class ShoppingCartRepository : IShoppingCartRepository
{
    public ShoppingCart? GetByCustomerId(int customerId)
    {
        //to do: Find kundens indkøbskurv i databasen
        throw new NotImplementedException();
    }

    public int Create(int customerId)
    {
        //to do Opret en ny indkøbskurv og returner kurvens id
        throw new NotImplementedException();
    }

    public List<Product> GetProducts(int shoppingCartId)
    {
        // to do: Hent produkterne fra indkøbskurven
        throw new NotImplementedException();
    }

    public int GetProductQuantity(int shoppingCartId, int productId)
    {
        // to do: Hent antallet af produktet i kurven
        throw new NotImplementedException();
    }

    public void AddProduct(int shoppingCartId, int productId, int quantity)
    {
        // To do Tilføj produktet til databasetabellen
        // ShoppingCartProducts
        throw new NotImplementedException();
    }

    public void UpdateQuantity(int shoppingCartId, int productId, int quantity)
    {
        //to do: Opdater antallet af produktet i kurven
        throw new NotImplementedException();
    }

    public void RemoveProduct(int shoppingCartId, int productId)
    {
        // to do: Fjern produktet fra indkøbskurven
        throw new NotImplementedException();
    }

    public void Clear(int shoppingCartId)
    {
        // to do: Fjern alle produkter fra indkøbskurven
        throw new NotImplementedException();
    }
}
