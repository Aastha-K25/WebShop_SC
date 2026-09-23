using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Obligatoris.opgave.Domain.Interfaces;
using Obligatoris.opgave.Domain.Models;

namespace Obligatorisk_opgave.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly string _connectionString;

    public ProductRepository(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("PaperPetalsDatabase")
            ?? throw new InvalidOperationException(
                "Database connection string was not found.");
    }

    public List<Product> GetAll()
    {
        // To do: Henter alle produkterne fra databasen
        throw new NotImplementedException();
    }

    public Product? GetById(int id)
    {
        // To do: Henter et produkt via id
        throw new NotImplementedException();
    }

    public List<Bouquet> GetAllBouquets()
    {
        // To do: Henter alle buketter fra databasen
        throw new NotImplementedException();
    }

    public List<Bouquet> GetPopularBouquets(int amount)
    {
        var bouquets = new List<Bouquet>();

        string sql = """
            SELECT TOP (@Amount)
                p.Id,
                p.Name,
                p.Description,
                p.Price,
                p.ImagePath,
                p.ImageDescription,
                p.IsPopular,
                p.StockQuantity,
                b.FlowerType,
                b.BouquetSize,
                b.BookGenre,
                b.NumberOfBooks
            FROM paperpetals.Products p
            INNER JOIN paperpetals.Bouquets b
                ON p.Id = b.ProductId
            WHERE p.IsPopular = 1
            ORDER BY p.Id;
            """;

        using SqlConnection connection =
            new SqlConnection(_connectionString);

        using SqlCommand command =
            new SqlCommand(sql, connection);

        command.Parameters.AddWithValue("@Amount", amount);

        connection.Open();

        using SqlDataReader reader = command.ExecuteReader();

        while (reader.Read())
        {
            Bouquet bouquet = new Bouquet
            {
                Id = reader.GetInt32(0),

                Name = reader.GetString(1),

                Description = reader.IsDBNull(2)
                    ? null
                    : reader.GetString(2),

                Price = reader.GetDecimal(3),

                ImagePath = reader.IsDBNull(4)
                    ? null
                    : reader.GetString(4),

                ImageDescription = reader.IsDBNull(5)
                    ? null
                    : reader.GetString(5),

                IsPopular = reader.GetBoolean(6),

                StockQuantity = reader.GetInt32(7),

                FlowerType = reader.GetString(8),

                BouquetSize = Enum.Parse<BouquetSize>(
                    reader.GetString(9)),

                BookGenre = Enum.Parse<BookGenre>(
                    reader.GetString(10)),

                NumberOfBooks = reader.GetInt32(11)
            };

            bouquets.Add(bouquet);
        }

        return bouquets;
    }

    public List<Bouquet> GetBouquetsByGenre(string genre)
    {
        // To do: Henter buketter ud fra genre
        throw new NotImplementedException();
    }

    public void Add(Product product)
    {
        // To do: Gemmer produkt i databasen
        throw new NotImplementedException();
    }

    public void Update(Product product)
    {
        // To do: Opdaterer produkt i databasen
        throw new NotImplementedException();
    }

    public void Delete(int id)
    {
        // To do: Sletter produkt fra databasen
        throw new NotImplementedException();
    }
}