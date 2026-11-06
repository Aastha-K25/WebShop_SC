
using Microsoft.Data.SqlClient; // Bruges til at arbejde med SQL Server
using Microsoft.Extensions.Configuration; // Bruges til at hente indstillinger
using Obligatoris.opgave.Domain.Interfaces; // Giver adgang til vores interfaces
using Obligatoris.opgave.Domain.Models; // Giver adgang til vores modeller

namespace Obligatorisk_opgave.Infrastructure.Repositories;

// Repository som håndterer indkøbskurven i databasen
public class ShoppingCartRepository : IShoppingCartRepository
{
    // Gemmer oplysningerne til forbindelsen med databasen
    private readonly string _connectionString;

    // Constructor som modtager vores konfiguration
    public ShoppingCartRepository(IConfiguration configuration)
    {
        // Henter connection string fra konfigurationen
        // Stopper med en fejl hvis den ikke findes
        _connectionString = configuration.GetConnectionString("PaperPetalsDatabase")
            ?? throw new InvalidOperationException("Database connection string was not found.");
    }

    // Finder en indkøbskurv ud fra kundens id
    public ShoppingCart? GetByCustomerId(int customerId)
    {
        // SQL som henter kundens indkøbskurv
        string sql = """
            SELECT
                Id,
                CustomerId
            FROM paperpetals.ShoppingCarts
            WHERE CustomerId = @CustomerId;
            """;

        // Opretter forbindelsen til databasen
        using SqlConnection connection = new SqlConnection(_connectionString);

        // Gør SQL forespørgslen klar
        using SqlCommand command = new SqlCommand(sql, connection);

        // Sender kundens id som parameter for at undgå SQL injection
        command.Parameters.AddWithValue("@CustomerId", customerId);

        // Åbner forbindelsen til databasen
        connection.Open();

        // Kører SQL og læser resultatet
        using SqlDataReader reader = command.ExecuteReader();

        // Tjekker om kunden har en indkøbskurv
        if (reader.Read())
        {
            // Opretter en ShoppingCart med oplysninger fra databasen
            return new ShoppingCart
            {
                // Henter kurvens id
                Id = reader.GetInt32(0),

                // Henter kundens id
                CustomerId = reader.GetInt32(1)
            };
        }

        // Returnerer null hvis kunden ikke har en kurv
        return null;
    }

    // Opretter en ny indkøbskurv til kunden
    public int Create(int customerId)
    {
        // SQL som opretter en indkøbskurv
        // OUTPUT INSERTED.Id henter det nye id
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

        // Opretter forbindelsen til databasen
        using SqlConnection connection = new SqlConnection(_connectionString);

        // Gør SQL forespørgslen klar
        using SqlCommand command = new SqlCommand(sql, connection);

        // Sender kundens id som parameter
        command.Parameters.AddWithValue("@CustomerId", customerId);

        // Åbner forbindelsen
        connection.Open();

        // Opretter kurven og henter dens nye id
        int shoppingCartId = Convert.ToInt32(command.ExecuteScalar());

        // Returnerer id på den nye indkøbskurv
        return shoppingCartId;
    }

    // Henter alle produkter som ligger i en bestemt indkøbskurv
    public List<Product> GetProducts(int shoppingCartId)
    {
        // Opretter en tom liste til produkterne
        List<Product> products = new List<Product>();

        // Henter produkterne fra kurven
        // INNER JOIN forbinder kurven med Products og Bouquets
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

        // Opretter forbindelsen til databasen
        using SqlConnection connection = new SqlConnection(_connectionString);

        // Gør SQL forespørgslen klar
        using SqlCommand command = new SqlCommand(sql, connection);

        // Sender kurvens id som parameter
        command.Parameters.AddWithValue("@ShoppingCartId", shoppingCartId);

        // Åbner forbindelsen
        connection.Open();

        // Kører SQL og læser resultaterne
        using SqlDataReader reader = command.ExecuteReader();

        // Går igennem alle produkterne i kurven
        while (reader.Read())
        {
            // Laver resultatet om til et Bouquet objekt
            Bouquet bouquet = CreateBouquet(reader);

            // Tilføjer buketten til listen med produkter
            // Det virker fordi Bouquet arver fra Product
            products.Add(bouquet);
        }

        // Returnerer listen med produkterne i kurven
        return products;
    }

    // Finder hvor mange der er af et bestemt produkt i kurven
    public int GetProductQuantity(int shoppingCartId, int productId)
    {
        // SQL som henter antallet af det valgte produkt
        string sql = """
            SELECT Quantity
            FROM paperpetals.ShoppingCartProducts
            WHERE ShoppingCartId = @ShoppingCartId
              AND ProductId = @ProductId;
            """;

        // Opretter forbindelsen til databasen
        using SqlConnection connection = new SqlConnection(_connectionString);

        // Gør SQL forespørgslen klar
        using SqlCommand command = new SqlCommand(sql, connection);

        // Sender kurvens id
        command.Parameters.AddWithValue("@ShoppingCartId", shoppingCartId);

        // Sender produktets id
        command.Parameters.AddWithValue("@ProductId", productId);

        // Åbner forbindelsen
        connection.Open();

        // Henter antallet fra databasen
        object? result = command.ExecuteScalar();

        // Kontrollerer om produktet findes i kurven
        if (result == null || result == DBNull.Value)
        {
            // Returnerer nul hvis produktet ikke findes
            return 0;
        }

        // Laver resultatet om til et heltal
        return Convert.ToInt32(result);
    }

    // Tilføjer et produkt til indkøbskurven
    public void AddProduct(int shoppingCartId, int productId, int quantity)
    {
        // Kontrollerer at antal er større end nul
        if (quantity <= 0)
        {
            // Stopper hvis antal er ugyldigt
            throw new ArgumentException("Quantity must be greater than zero.");
        }

        // SQL som gemmer produktet i indkøbskurven
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

        // Opretter forbindelsen til databasen
        using SqlConnection connection = new SqlConnection(_connectionString);

        // Gør SQL forespørgslen klar
        using SqlCommand command = new SqlCommand(sql, connection);

        // Sender kurvens id
        command.Parameters.AddWithValue("@ShoppingCartId", shoppingCartId);

        // Sender produktets id
        command.Parameters.AddWithValue("@ProductId", productId);

        // Sender hvor mange der skal tilføjes
        command.Parameters.AddWithValue("@Quantity", quantity);

        // Åbner forbindelsen
        connection.Open();

        // Kører SQL og gemmer produktet i kurven
        command.ExecuteNonQuery();
    }

    // Ændrer antallet af et produkt som allerede ligger i kurven
    public void UpdateQuantity(int shoppingCartId, int productId, int quantity)
    {
        // Kontrollerer at antal er større end nul
        if (quantity <= 0)
        {
            // Stopper hvis antal er ugyldigt
            throw new ArgumentException("Quantity must be greater than zero.");
        }

        // SQL som ændrer antallet af det valgte produkt
        string sql = """
            UPDATE paperpetals.ShoppingCartProducts
            SET Quantity = @Quantity
            WHERE ShoppingCartId = @ShoppingCartId
              AND ProductId = @ProductId;
            """;

        // Opretter forbindelsen til databasen
        using SqlConnection connection = new SqlConnection(_connectionString);

        // Gør SQL forespørgslen klar
        using SqlCommand command = new SqlCommand(sql, connection);

        // Sender kurvens id
        command.Parameters.AddWithValue("@ShoppingCartId", shoppingCartId);

        // Sender produktets id
        command.Parameters.AddWithValue("@ProductId", productId);

        // Sender det nye antal
        command.Parameters.AddWithValue("@Quantity", quantity);

        // Åbner forbindelsen
        connection.Open();

        // Kører SQL og opdaterer antallet
        command.ExecuteNonQuery();
    }

    // Fjerner et bestemt produkt fra indkøbskurven
    public void RemoveProduct(int shoppingCartId, int productId)
    {
        // SQL som fjerner produktet fra den valgte kurv
        string sql = """
            DELETE FROM paperpetals.ShoppingCartProducts
            WHERE ShoppingCartId = @ShoppingCartId
              AND ProductId = @ProductId;
            """;

        // Opretter forbindelsen til databasen
        using SqlConnection connection = new SqlConnection(_connectionString);

        // Gør SQL forespørgslen klar
        using SqlCommand command = new SqlCommand(sql, connection);

        // Sender kurvens id
        command.Parameters.AddWithValue("@ShoppingCartId", shoppingCartId);

        // Sender produktets id
        command.Parameters.AddWithValue("@ProductId", productId);

        // Åbner forbindelsen
        connection.Open();

        // Kører SQL og fjerner produktet fra kurven
        command.ExecuteNonQuery();
    }

    // Tømmer hele indkøbskurven
    public void Clear(int shoppingCartId)
    {
        // SQL som fjerner alle produkter fra kurven
        // Selve indkøbskurven bliver ikke slettet
        string sql = """
            DELETE FROM paperpetals.ShoppingCartProducts
            WHERE ShoppingCartId = @ShoppingCartId;
            """;

        // Opretter forbindelsen til databasen
        using SqlConnection connection = new SqlConnection(_connectionString);

        // Gør SQL forespørgslen klar
        using SqlCommand command = new SqlCommand(sql, connection);

        // Sender id på kurven som skal tømmes
        command.Parameters.AddWithValue("@ShoppingCartId", shoppingCartId);

        // Åbner forbindelsen
        connection.Open();

        // Kører SQL og fjerner produkterne fra kurven
        command.ExecuteNonQuery();
    }

    // Hjælpemetode som laver en række fra databasen om til en Bouquet
    private static Bouquet CreateBouquet(SqlDataReader reader)
    {
        // Opretter et nyt Bouquet objekt med værdier fra databasen
        return new Bouquet
        {
            // Henter produktets id fra første kolonne
            Id = reader.GetInt32(0),

            // Henter produktets navn fra anden kolonne
            Name = reader.GetString(1),

            // Henter beskrivelsen eller null hvis den mangler
            Description = reader.IsDBNull(2)
                ? null
                : reader.GetString(2),

            // Henter produktets pris
            Price = reader.GetDecimal(3),

            // Henter billedets sti eller null hvis den mangler
            ImagePath = reader.IsDBNull(4)
                ? null
                : reader.GetString(4),

            // Henter billedets beskrivelse eller null
            ImageDescription = reader.IsDBNull(5)
                ? null
                : reader.GetString(5),

            // Henter om produktet er markeret som populært
            IsPopular = reader.GetBoolean(6),

            // Henter hvor mange der er på lager
            StockQuantity = reader.GetInt32(7),

            // Henter hvilken type blomster buketten indeholder
            FlowerType = reader.GetString(8),

            // Laver teksten fra databasen om til BouquetSize enum
            BouquetSize = Enum.Parse<BouquetSize>(reader.GetString(9)),

            // Laver teksten fra databasen om til BookGenre enum
            BookGenre = Enum.Parse<BookGenre>(reader.GetString(10)),

            // Henter antallet af bøger i buketten
            NumberOfBooks = reader.GetInt32(11)
        };
    }
}
