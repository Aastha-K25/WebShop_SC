using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Obligatoris.opgave.Domain.Interfaces;
using Obligatoris.opgave.Domain.Models;

namespace Obligatorisk_opgave.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    // Connection stringen fortæller, hvor databasen ligger, og hvordan programmet opretter forbindelse til den.
    private readonly string _connectionString;


    // IConfiguration bruges til at hente connection stringen fra User Secrets.
    public ProductRepository(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("PaperPetalsDatabase")
            ?? throw new InvalidOperationException(
                "Database connection string was not found.");
    }


    // Henter alle produkter fra databasen. Lige nu er Bouquet den eneste type Product, vi har.
    public List<Product> GetAll()
    {
        List<Bouquet> bouquets = GetAllBouquets();

        // Bouquet arver fra Product.Derfor kan listen med buketter omdannes til en liste med produkter.
        return bouquets.Cast<Product>().ToList();
    }


    // Finder ét bestemt produkt ud fra produktets id.
    public Product? GetById(int id)
    {
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
                     FROM paperpetals.Products p
                     INNER JOIN paperpetals.Bouquets b
                         ON p.Id = b.ProductId
                     WHERE p.Id = @Id;
                     """;

        // Forbindelsen bliver automatisk lukket igen, fordi vi bruger using.
        using SqlConnection connection =
            new SqlConnection(_connectionString);

        using SqlCommand command =
            new SqlCommand(sql, connection);

        // Vi bruger en parameter i stedet for at sætte id direkte ind i SQL-strengen. Det beskytter mod SQL Injection.
        command.Parameters.AddWithValue("@Id", id);

        connection.Open();

        using SqlDataReader reader = command.ExecuteReader();

        // Hvis databasen finder produktet, bliver databaserækken lavet om til et Bouquet-objekt.
        if (reader.Read())
        {
            return CreateBouquet(reader);
        }

        // Hvis databasen ikke finder et produkt med id'et, returnerer vi null.
        return null;
    }


    // Henter alle buketter fra databasen.
    public List<Bouquet> GetAllBouquets()
    {
        List<Bouquet> bouquets = new List<Bouquet>();

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
                     FROM paperpetals.Products p
                     INNER JOIN paperpetals.Bouquets b
                         ON p.Id = b.ProductId
                     ORDER BY p.Name;
                     """;

        using SqlConnection connection =
            new SqlConnection(_connectionString);

        using SqlCommand command =
            new SqlCommand(sql, connection);

        connection.Open();

        using SqlDataReader reader = command.ExecuteReader();

        // While-løkken kører én gang for hver række, som databasen returnerer.
        while (reader.Read())
        {
            Bouquet bouquet = CreateBouquet(reader);

            bouquets.Add(bouquet);
        }

        return bouquets;
    }


    // Henter et bestemt antal populære buketter. Forsiden kan eksempelvis bede om de første fire.
    public List<Bouquet> GetPopularBouquets(int amount)
    {
        List<Bouquet> bouquets = new List<Bouquet>();

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
            Bouquet bouquet = CreateBouquet(reader);

            bouquets.Add(bouquet);
        }

        return bouquets;
    }


    // Henter alle buketter fra en bestemt boggenre.
    public List<Bouquet> GetBouquetsByGenre(BookGenre genre)
    {
        List<Bouquet> bouquets = new List<Bouquet>();

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
                     FROM paperpetals.Products p
                     INNER JOIN paperpetals.Bouquets b
                         ON p.Id = b.ProductId
                     WHERE b.BookGenre = @BookGenre
                     ORDER BY p.Name;
                     """;

        using SqlConnection connection =
            new SqlConnection(_connectionString);

        using SqlCommand command =
            new SqlCommand(sql, connection);

        // En enum bliver gemt som tekst i databasen. Eksempel: Romance eller Fantasy.
        command.Parameters.AddWithValue(
            "@BookGenre",
            genre.ToString());

        connection.Open();

        using SqlDataReader reader = command.ExecuteReader();

        while (reader.Read())
        {
            Bouquet bouquet = CreateBouquet(reader);

            bouquets.Add(bouquet);
        }

        return bouquets;
    }


    // Gemmer et nyt produkt i databasen.
    public void Add(Product product)
    {
        // Vi har kun Bouquet som produkttype lige nu. Derfor kontrollerer vi, at produktet faktisk er en buket.
        if (product is not Bouquet bouquet)
        {
            throw new ArgumentException(
                "The product must be a bouquet.");
        }

        using SqlConnection connection =
            new SqlConnection(_connectionString);

        connection.Open();

        // En Bouquet bliver gemt i to tabeller:
        // - Products indeholder de fælles produktoplysninger.
        // - Bouquets indeholder oplysninger, som kun gælder buketter.
        // En transaction sikrer, at begge INSERT-statements gennemføres. Hvis den ene fejler, gemmes den anden heller ikke.
        using SqlTransaction transaction =
            connection.BeginTransaction();

        try
        {
            string productSql = """
                                INSERT INTO paperpetals.Products
                                (
                                    Name,
                                    Description,
                                    Price,
                                    ImagePath,
                                    ImageDescription,
                                    IsPopular,
                                    StockQuantity
                                )
                                OUTPUT INSERTED.Id
                                VALUES
                                (
                                    @Name,
                                    @Description,
                                    @Price,
                                    @ImagePath,
                                    @ImageDescription,
                                    @IsPopular,
                                    @StockQuantity
                                );
                                """;

            using SqlCommand productCommand =
                new SqlCommand(
                    productSql,
                    connection,
                    transaction);

            productCommand.Parameters.AddWithValue(
                "@Name",
                bouquet.Name);

            // Description må gerne være null.
            productCommand.Parameters.AddWithValue(
                "@Description",
                (object?)bouquet.Description ?? DBNull.Value);

            productCommand.Parameters.AddWithValue(
                "@Price",
                bouquet.Price);

            // ImagePath må gerne være null.
            productCommand.Parameters.AddWithValue(
                "@ImagePath",
                (object?)bouquet.ImagePath ?? DBNull.Value);

            // ImageDescription må gerne være null.
            productCommand.Parameters.AddWithValue(
                "@ImageDescription",
                (object?)bouquet.ImageDescription ?? DBNull.Value);

            productCommand.Parameters.AddWithValue(
                "@IsPopular",
                bouquet.IsPopular);

            productCommand.Parameters.AddWithValue(
                "@StockQuantity",
                bouquet.StockQuantity);

            // SQL Server opretter produktets id. OUTPUT INSERTED.Id returnerer det nye id til C#.
            int productId =
                Convert.ToInt32(productCommand.ExecuteScalar());


            string bouquetSql = """
                                INSERT INTO paperpetals.Bouquets
                                (
                                    ProductId,
                                    FlowerType,
                                    BouquetSize,
                                    BookGenre,
                                    NumberOfBooks
                                )
                                VALUES
                                (
                                    @ProductId,
                                    @FlowerType,
                                    @BouquetSize,
                                    @BookGenre,
                                    @NumberOfBooks
                                );
                                """;

            using SqlCommand bouquetCommand =
                new SqlCommand(
                    bouquetSql,
                    connection,
                    transaction);

            bouquetCommand.Parameters.AddWithValue(
                "@ProductId",
                productId);

            bouquetCommand.Parameters.AddWithValue(
                "@FlowerType",
                bouquet.FlowerType);

            bouquetCommand.Parameters.AddWithValue(
                "@BouquetSize",
                bouquet.BouquetSize.ToString());

            bouquetCommand.Parameters.AddWithValue(
                "@BookGenre",
                bouquet.BookGenre.ToString());

            bouquetCommand.Parameters.AddWithValue(
                "@NumberOfBooks",
                bouquet.NumberOfBooks);

            bouquetCommand.ExecuteNonQuery();

            // Begge dele er gemt korrekt.
            transaction.Commit();

            // Det nye database-id gemmes også i objektet.
            bouquet.Id = productId;
        }
        catch
        {
            // Hvis der opstår en fejl, bliver begge ændringer fortrudt.
            transaction.Rollback();

            // Fejlen sendes videre, så den senere kan logges.
            throw;
        }
    }


    // Opdaterer et eksisterende produkt.
    public void Update(Product product)
    {
        if (product is not Bouquet bouquet)
        {
            throw new ArgumentException(
                "The product must be a bouquet.");
        }

        using SqlConnection connection =
            new SqlConnection(_connectionString);

        connection.Open();

        // Oplysningerne skal opdateres i både Products og Bouquets. Derfor bruger vi igen en transaction.
        using SqlTransaction transaction =
            connection.BeginTransaction();

        try
        {
            string productSql = """
                                UPDATE paperpetals.Products
                                SET
                                    Name = @Name,
                                    Description = @Description,
                                    Price = @Price,
                                    ImagePath = @ImagePath,
                                    ImageDescription = @ImageDescription,
                                    IsPopular = @IsPopular,
                                    StockQuantity = @StockQuantity
                                WHERE Id = @Id;
                                """;

            using SqlCommand productCommand =
                new SqlCommand(
                    productSql,
                    connection,
                    transaction);

            productCommand.Parameters.AddWithValue(
                "@Id",
                bouquet.Id);

            productCommand.Parameters.AddWithValue(
                "@Name",
                bouquet.Name);

            productCommand.Parameters.AddWithValue(
                "@Description",
                (object?)bouquet.Description ?? DBNull.Value);

            productCommand.Parameters.AddWithValue(
                "@Price",
                bouquet.Price);

            productCommand.Parameters.AddWithValue(
                "@ImagePath",
                (object?)bouquet.ImagePath ?? DBNull.Value);

            productCommand.Parameters.AddWithValue(
                "@ImageDescription",
                (object?)bouquet.ImageDescription ?? DBNull.Value);

            productCommand.Parameters.AddWithValue(
                "@IsPopular",
                bouquet.IsPopular);

            productCommand.Parameters.AddWithValue(
                "@StockQuantity",
                bouquet.StockQuantity);

            productCommand.ExecuteNonQuery();


            string bouquetSql = """
                                UPDATE paperpetals.Bouquets
                                SET
                                    FlowerType = @FlowerType,
                                    BouquetSize = @BouquetSize,
                                    BookGenre = @BookGenre,
                                    NumberOfBooks = @NumberOfBooks
                                WHERE ProductId = @ProductId;
                                """;

            using SqlCommand bouquetCommand =
                new SqlCommand(
                    bouquetSql,
                    connection,
                    transaction);

            bouquetCommand.Parameters.AddWithValue(
                "@ProductId",
                bouquet.Id);

            bouquetCommand.Parameters.AddWithValue(
                "@FlowerType",
                bouquet.FlowerType);

            bouquetCommand.Parameters.AddWithValue(
                "@BouquetSize",
                bouquet.BouquetSize.ToString());

            bouquetCommand.Parameters.AddWithValue(
                "@BookGenre",
                bouquet.BookGenre.ToString());

            bouquetCommand.Parameters.AddWithValue(
                "@NumberOfBooks",
                bouquet.NumberOfBooks);

            bouquetCommand.ExecuteNonQuery();

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }


    // Sletter et produkt ud fra produktets id.
    public void Delete(int id)
    {
        using SqlConnection connection =
            new SqlConnection(_connectionString);

        connection.Open();

        using SqlTransaction transaction =
            connection.BeginTransaction();

        try
        {
            // Bouquet-tabellen refererer til Products-tabellen. Derfor skal bouquet-oplysningerne slettes først.
            string bouquetSql = """
                                DELETE FROM paperpetals.Bouquets
                                WHERE ProductId = @ProductId;
                                """;

            using SqlCommand bouquetCommand =
                new SqlCommand(
                    bouquetSql,
                    connection,
                    transaction);

            bouquetCommand.Parameters.AddWithValue(
                "@ProductId",
                id);

            bouquetCommand.ExecuteNonQuery();


            // Nu kan de fælles produktoplysninger slettes.
            string productSql = """
                                DELETE FROM paperpetals.Products
                                WHERE Id = @Id;
                                """;

            using SqlCommand productCommand =
                new SqlCommand(
                    productSql,
                    connection,
                    transaction);

            productCommand.Parameters.AddWithValue(
                "@Id",
                id);

            productCommand.ExecuteNonQuery();

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }


    // Denne hjælpemetode bruges af flere af Get-metoderne. Den omdanner én række fra databasen til et Bouquet-objekt.
    private static Bouquet CreateBouquet(
        SqlDataReader reader)
    {
        return new Bouquet
        {
            Id = reader.GetInt32(0),

            Name = reader.GetString(1),

            // IsDBNull kontrollerer, om værdien i databasen er null.
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

            // Teksten fra databasen bliver lavet om til BouquetSize-enum.
            BouquetSize = Enum.Parse<BouquetSize>(
                reader.GetString(9)),

            // Teksten fra databasen bliver lavet om til BookGenre-enum.
            BookGenre = Enum.Parse<BookGenre>(
                reader.GetString(10)),

            NumberOfBooks = reader.GetInt32(11)
        };
    }
}