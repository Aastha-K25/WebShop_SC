using Obligatoris.opgave.Domain.Interfaces;
using Obligatoris.opgave.Domain.Models;

namespace Obligatorisk_opgave.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    public List<Product> GetAll()
    {
        //To do henter alle produkterne fra db - Fikses med db
        throw new NotImplementedException();
    }

    public Product? GetById(int id)
    {
        //to do henter alle produkerten via ID - Fikses med db
        throw new NotImplementedException();
    }

    public List<Bouquet> GetAllBouquets()
    {
        //TO do henter alle buketteren fra db - Fikses med db
        throw new NotImplementedException();
    }

    public List<Bouquet> GetPopularBouquets(int amount)
    {
        //To Do henter alle populære bukketter fra db- Fikses med db
        throw new NotImplementedException();
    }

    public List<Bouquet> GetBouquetsByGenre(string genre)
    {
        // To do henter alle bukketeren ud fra deres genre - Fikses med db
        throw new NotImplementedException();
    }

    public void Add(Product product)
    {
        //To DO gemmer produkter i db - Fikses med db
        throw new NotImplementedException();
    }

    public void Update(Product product)
    {
        //To do updatere produkterne i db - Fikses med db
        throw new NotImplementedException();
    }

    public void Delete(int id)
    {
        //To do sletter alle produkteren i db - Fikses med db
        throw new NotImplementedException();
    }
}