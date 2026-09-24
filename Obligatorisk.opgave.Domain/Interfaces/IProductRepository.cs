using Obligatoris.opgave.Domain.Models;

namespace Obligatoris.opgave.Domain.Interfaces;

// Interfacet bestemmer, hvad et produkt-repository skal kunne.
// Den rigtige SQL-kode bliver senere skrevet i ProductRepository.
public interface IProductRepository
{
    // Henter alle produkter fra databasen
    List<Product> GetAll();

    // Henter ét produkt ud fra produktets id
    Product? GetById(int id);

    // Henter alle buketter fra databasen
    List<Bouquet> GetAllBouquets();

    // Henter et bestemt antal populære buketter
    List<Bouquet> GetPopularBouquets(int amount);

    // Henter buketter fra en bestemt genre
    List<Bouquet> GetBouquetsByGenre(BookGenre genre);

    // Gemmer et nyt produkt
    void Add(Product product);

    // Opdaterer et eksisterende produkt
    void Update(Product product);

    // Sletter et produkt ud fra id
    void Delete(int id);
}