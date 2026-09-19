using Obligatoris.opgave.Domain.Interfaces;
using Obligatoris.opgave.Domain.Models;

namespace Obligatorisk_opgave.Infrastructure.Repositories;

public class OrderRepository
{
    public Order? GetById(int id)
    {
        // To do: Hent en ordre ud fra ordrens id
        throw new NotImplementedException();
    }
    public List<Order> GetByCustomerId(int customerId)
    {
        // TO do: Hent alle ordrer, som tilhører kunden
        throw new NotImplementedException();
    }

    public int Add(Order order)
    {
        // to do: Gem ordren og returner det nye ordre-id
        throw new NotImplementedException();
    }

    public void AddProductToOrder(
        int orderId,
        int productId,
        int quantity,
        decimal priceAtPurchase)
    {
        // To do: Gem forbindelsen mellem ordren og produktet
        // i databasetabellen OrderProducts
        throw new NotImplementedException();
    }

    public void UpdateStatus(int orderId, string status)
    {
        //to do: Opdater ordrens status i databasen
        throw new NotImplementedException();
    }
}