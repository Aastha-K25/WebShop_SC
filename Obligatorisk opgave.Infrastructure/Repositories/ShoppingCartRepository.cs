using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Obligatoris.opgave.Domain.Interfaces;
using Obligatoris.opgave.Domain.Models;

namespace Obligatorisk_opgave.Infrastructure.Repositories;

public class ShoppingCartRepository : IShoppingCartRepository
{
    // Connection stringen bruges til at oprette forbindelse
    // til SQL Server-databasen.
    private readonly string _connectionString;


    // Connection stringen hentes fra User Secrets.
    public ShoppingCartRepository(
        IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("PaperPetalsDatabase")
            ?? throw new InvalidOperationException(
                "Database connection string was not found.");
    }


    // Finder en indkøbskurv ud fra kundens id.
    public ShoppingCart? GetByCustomerId(int customerId)
    {
        string sql = """
            SELECT
                Id,
                CustomerId
            FROM paperpetals.ShoppingCarts
            WHERE CustomerId = @CustomerId;
            """;

        using SqlConnection connection =
            new SqlConnection(_connectionString);

        using SqlCommand command =
            new SqlCommand(sql, connection);

        command.Parameters.AddWithValue(
            "@CustomerId",
            customerId);

        connection.Open();

        using SqlDataReader reader = command.ExecuteReader();

        if (reader.Read())
        {
            return new ShoppingCart
            {
                Id = reader.GetInt32(0),
                CustomerId = reader.GetInt32(1)
            };
        }

        // Hvis kunden ikke har en kurv endnu,
        // returnerer vi null.
        return null;
    }


    // Opretter en ny indkøbskurv til kunden.
    // Metoden returnerer det id, som SQL Server opretter.
    public int Create(int customerId)
    {
        string sql = """
            INSERT INTO paperpetals.ShoppingCarts
            (
                CustomerId
            )
            OUTPUT INSERTED.Id
            VALUES
            (
                @CustomerId
            );
            """;

        using SqlConnection connection =
            new SqlConnection(_connectionString);

        using SqlCommand command =
            new SqlCommand(sql, connection);

        command.Parameters.AddWithValue(
            "@CustomerId",
            customerId);

        connection.Open();

        int shoppingCartId =
            Convert.ToInt32(command.ExecuteScalar());

        return shoppingCartId;
    }


    // Henter alle produkter, som ligger i en bestemt kurv.
    public List<Product> GetProducts(int shoppingCartId)
    {
        List<Product> products = new List<Product>();

        string sql = """
            SELECT
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
            FROM paperpetals.ShoppingCartProducts scp
            INNER JOIN paperpetals.Products p
                ON scp.ProductId = p.Id
            INNER JOIN paperpetals.Bouquets b
                ON p.Id = b.ProductId
            WHERE scp.ShoppingCartId = @ShoppingCartId
            ORDER BY p.Name;
            """;

        using SqlConnection connection =
            new SqlConnection(_connectionString);

        using SqlCommand command =
            new SqlCommand(sql, connection);

        command.Parameters.AddWithValue(
            "@ShoppingCartId",
            shoppingCartId);

        connection.Open();

        using SqlDataReader reader = command.ExecuteReader();

        while (reader.Read())
        {
            Bouquet bouquet = CreateBouquet(reader);

            // Bouquet arver fra Product.
            // Derfor kan buketten gemmes i listen med Product.
            products.Add(bouquet);
        }

        return products;
    }


    // Finder antallet af et bestemt produkt i kurven.
    public int GetProductQuantity(
        int shoppingCartId,
        int productId)
    {
        string sql = """
            SELECT Quantity
            FROM paperpetals.ShoppingCartProducts
            WHERE ShoppingCartId = @ShoppingCartId
              AND ProductId = @ProductId;
            """;

        using SqlConnection connection =
            new SqlConnection(_connectionString);

        using SqlCommand command =
            new SqlCommand(sql, connection);

        command.Parameters.AddWithValue(
            "@ShoppingCartId",
            shoppingCartId);

        command.Parameters.AddWithValue(
            "@ProductId",
            productId);

        connection.Open();

        object? result = command.ExecuteScalar();

        // Hvis produktet ikke ligger i kurven,
        // returnerer ExecuteScalar null.
        if (result == null || result == DBNull.Value)
        {
            return 0;
        }

        return Convert.ToInt32(result);
    }


    // Tilføjer et nyt produkt til indkøbskurven.
    public void AddProduct(
        int shoppingCartId,
        int productId,
        int quantity)
    {
        // Det skal ikke være muligt at tilføje
        // 0 eller et negativt antal produkter.
        if (quantity <= 0)
        {
            throw new ArgumentException(
                "Quantity must be greater than zero.");
        }

        string sql = """
            INSERT INTO paperpetals.ShoppingCartProducts
            (
                ShoppingCartId,
                ProductId,
                Quantity
            )
            VALUES
            (
                @ShoppingCartId,
                @ProductId,
                @Quantity
            );
            """;

        using SqlConnection connection =
            new SqlConnection(_connectionString);

        using SqlCommand command =
            new SqlCommand(sql, connection);

        command.Parameters.AddWithValue(
            "@ShoppingCartId",
            shoppingCartId);

        command.Parameters.AddWithValue(
            "@ProductId",
            productId);

        command.Parameters.AddWithValue(
            "@Quantity",
            quantity);

        connection.Open();

        command.ExecuteNonQuery();
    }


    // Ændrer antallet af et produkt, som allerede ligger i kurven.
    public void UpdateQuantity(
        int shoppingCartId,
        int productId,
        int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException(
                "Quantity must be greater than zero.");
        }

        string sql = """
            UPDATE paperpetals.ShoppingCartProducts
            SET Quantity = @Quantity
            WHERE ShoppingCartId = @ShoppingCartId
              AND ProductId = @ProductId;
            """;

        using SqlConnection connection =
            new SqlConnection(_connectionString);

        using SqlCommand command =
            new SqlCommand(sql, connection);

        command.Parameters.AddWithValue(
            "@ShoppingCartId",
            shoppingCartId);

        command.Parameters.AddWithValue(
            "@ProductId",
            productId);

        command.Parameters.AddWithValue(
            "@Quantity",
            quantity);

        connection.Open();

        command.ExecuteNonQuery();
    }


    // Fjerner ét bestemt produkt fra kurven.
    public void RemoveProduct(
        int shoppingCartId,
        int productId)
    {
        string sql = """
            DELETE FROM paperpetals.ShoppingCartProducts
            WHERE ShoppingCartId = @ShoppingCartId
              AND ProductId = @ProductId;
            """;

        using SqlConnection connection =
            new SqlConnection(_connectionString);

        using SqlCommand command =
            new SqlCommand(sql, connection);

        command.Parameters.AddWithValue(
            "@ShoppingCartId",
            shoppingCartId);

        command.Parameters.AddWithValue(
            "@ProductId",
            productId);

        connection.Open();

        command.ExecuteNonQuery();
    }


    // Tømmer hele indkøbskurven.
    //
    // Selve ShoppingCart-rækken bliver ikke slettet.
    // Det er kun forbindelserne til produkterne, der fjernes.
    public void Clear(int shoppingCartId)
    {
        string sql = """
            DELETE FROM paperpetals.ShoppingCartProducts
            WHERE ShoppingCartId = @ShoppingCartId;
            """;

        using SqlConnection connection =
            new SqlConnection(_connectionString);

        using SqlCommand command =
            new SqlCommand(sql, connection);

        command.Parameters.AddWithValue(
            "@ShoppingCartId",
            shoppingCartId);

        connection.Open();

        command.ExecuteNonQuery();
    }


    // Hjælpemetoden omdanner én række fra databasen
    // til et Bouquet-objekt.
    private static Bouquet CreateBouquet(
        SqlDataReader reader)
    {
        return new Bouquet
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
    }
}