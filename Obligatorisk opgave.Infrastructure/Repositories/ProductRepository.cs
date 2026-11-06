
using Microsoft.Data.SqlClient; // Bruges til at arbejde med SQL Server
using Microsoft.Extensions.Configuration; // Bruges til at hente indstillinger
using Obligatoris.opgave.Domain.Interfaces; // Giver adgang til vores interfaces
using Obligatoris.opgave.Domain.Models; // Giver adgang til vores modeller

namespace Obligatorisk_opgave.Infrastructure.Repositories;

// Repository som håndterer vores produkter i databasen
public class ProductRepository : IProductRepository
{
    // Gemmer forbindelsesoplysningerne til databasen
    private readonly string _connectionString;

    // Constructor som modtager vores konfiguration
    public ProductRepository(IConfiguration configuration)
    {
        // Henter connection string fra konfigurationen
        // Stopper med en fejl hvis den ikke findes
        _connectionString = configuration.GetConnectionString("PaperPetalsDatabase")
            ?? throw new InvalidOperationException("Database connection string was not found.");
    }

    // Henter alle produkter fra databasen
    public List<Product> GetAll()
    {
        // Henter alle buketter og gemmer dem i en liste
        List<Bouquet> bouquets = GetAllBouquets();

        // Laver listen med Bouquet om til en liste med Product
        // Det virker fordi Bouquet arver fra Product
        return bouquets.Cast<Product>().ToList();
    }

    // Finder et bestemt produkt ud fra dets id
    public Product? GetById(int id)
    {
        // SQL som henter oplysninger fra både Products og Bouquets
        // INNER JOIN forbinder tabellerne gennem produktets id
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

        // Opretter forbindelsen til databasen
        // using sørger for at forbindelsen bliver lukket igen
        using SqlConnection connection = new SqlConnection(_connectionString);

        // Gør SQL forespørgslen klar
        using SqlCommand command = new SqlCommand(sql, connection);

        // Sender produktets id som parameter for at undgå SQL injection
        command.Parameters.AddWithValue("@Id", id);

        // Åbner forbindelsen til databasen
        connection.Open();

        // Kører SQL og læser resultatet
        using SqlDataReader reader = command.ExecuteReader();

        // Tjekker om databasen fandt produktet
        if (reader.Read())
        {
            // Laver resultatet om til et Bouquet objekt
            return CreateBouquet(reader);
        }

        // Returnerer null hvis produktet ikke findes
        return null;
    }

    // Henter alle buketter fra databasen
    public List<Bouquet> GetAllBouquets()
    {
        // Opretter en tom liste til buketterne
        List<Bouquet> bouquets = new List<Bouquet>();

        // Henter alle buketter og sorterer dem efter navn
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

        // Opretter forbindelsen til databasen
        using SqlConnection connection = new SqlConnection(_connectionString);

        // Gør SQL forespørgslen klar
        using SqlCommand command = new SqlCommand(sql, connection);

        // Åbner forbindelsen
        connection.Open();

        // Kører SQL og henter resultaterne
        using SqlDataReader reader = command.ExecuteReader();

        // Går igennem alle buketter som blev fundet
        while (reader.Read())
        {
            // Laver rækken fra databasen om til en Bouquet
            Bouquet bouquet = CreateBouquet(reader);

            // Tilføjer buketten til listen
            bouquets.Add(bouquet);
        }

        // Returnerer listen med alle buketter
        return bouquets;
    }

    // Henter et bestemt antal populære buketter
    public List<Bouquet> GetPopularBouquets(int amount)
    {
        // Opretter en tom liste til de populære buketter
        List<Bouquet> bouquets = new List<Bouquet>();

        // TOP bestemmer hvor mange buketter der skal hentes
        // IsPopular = 1 betyder at produktet er markeret som populært
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

        // Opretter forbindelsen til databasen
        using SqlConnection connection = new SqlConnection(_connectionString);

        // Gør SQL forespørgslen klar
        using SqlCommand command = new SqlCommand(sql, connection);

        // Sender hvor mange populære buketter vi vil hente
        command.Parameters.AddWithValue("@Amount", amount);

        // Åbner forbindelsen
        connection.Open();

        // Kører SQL og læser resultaterne
        using SqlDataReader reader = command.ExecuteReader();

        // Går igennem alle fundne buketter
        while (reader.Read())
        {
            // Laver resultatet om til et Bouquet objekt
            Bouquet bouquet = CreateBouquet(reader);

            // Tilføjer buketten til listen
            bouquets.Add(bouquet);
        }

        // Returnerer de populære buketter
        return bouquets;
    }

    // Henter buketter som passer til en bestemt boggenre
    public List<Bouquet> GetBouquetsByGenre(BookGenre genre)
    {
        // Opretter en tom liste til buketterne
        List<Bouquet> bouquets = new List<Bouquet>();

        // SQL som finder buketter med den valgte genre
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

        // Opretter forbindelsen til databasen
        using SqlConnection connection = new SqlConnection(_connectionString);

        // Gør SQL forespørgslen klar
        using SqlCommand command = new SqlCommand(sql, connection);

        // Laver vores enum om til tekst som databasen kan sammenligne
        command.Parameters.AddWithValue("@BookGenre", genre.ToString());

        // Åbner forbindelsen
        connection.Open();

        // Kører SQL og læser resultaterne
        using SqlDataReader reader = command.ExecuteReader();

        // Går igennem alle buketter med den valgte genre
        while (reader.Read())
        {
            // Laver resultatet om til et Bouquet objekt
            Bouquet bouquet = CreateBouquet(reader);

            // Tilføjer buketten til listen
            bouquets.Add(bouquet);
        }

        // Returnerer listen med buketterne
        return bouquets;
    }

    // Søger efter buketter ud fra navn eller beskrivelse
    public List<Bouquet> SearchBouquets(string searchTerm)
    {
        // Opretter en tom liste til søgeresultaterne
        List<Bouquet> bouquets = new List<Bouquet>();

        // Kontrollerer om søgefeltet er tomt eller kun indeholder mellemrum
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            // Viser alle buketter hvis der ikke er skrevet noget
            return GetAllBouquets();
        }

        // SQL som søger i både produktets navn og beskrivelse
        // LIKE bruges til at finde tekst som matcher søgningen
        string sql = """
            SELECT p.Id, p.Name, p.Description, p.Price,
                   p.ImagePath, p.ImageDescription, p.IsPopular,
                   p.StockQuantity, b.FlowerType, b.BouquetSize,
                   b.BookGenre, b.NumberOfBooks
            FROM paperpetals.Products p
            INNER JOIN paperpetals.Bouquets b ON p.Id = b.ProductId
            WHERE p.Name LIKE @SearchTerm
               OR p.Description LIKE @SearchTerm
            ORDER BY p.Name;
            """;

        // Opretter forbindelsen til databasen
        using SqlConnection connection = new SqlConnection(_connectionString);

        // Gør SQL forespørgslen klar
        using SqlCommand command = new SqlCommand(sql, connection);

        // Sender søgeteksten som en SQL parameter
        // NVarChar bruges til tekst og 202 angiver den maksimale længde
        // Trim fjerner mellemrum i starten og slutningen
        // Procenttegnene gør at vi kan finde teksten inde i et navn
        command.Parameters.Add(
            "@SearchTerm", System.Data.SqlDbType.NVarChar, 202
        ).Value = "%" + searchTerm.Trim() + "%";

        // Åbner forbindelsen
        connection.Open();

        // Kører SQL og læser søgeresultaterne
        using SqlDataReader reader = command.ExecuteReader();

        // Går igennem alle produkter som matcher søgningen
        while (reader.Read())
        {
            // Laver produktet om til en Bouquet og tilføjer det til listen
            bouquets.Add(CreateBouquet(reader));
        }

        // Returnerer søgeresultaterne
        return bouquets;
    }

    // Gemmer et nyt produkt i databasen
    public void Add(Product product)
    {
        // Kontrollerer at produktet er en Bouquet
        // Gemmer samtidig produktet i variablen bouquet
        if (product is not Bouquet bouquet)
        {
            // Stopper hvis produktet ikke er en buket
            throw new ArgumentException("The product must be a bouquet.");
        }

        // Opretter forbindelsen til databasen
        using SqlConnection connection = new SqlConnection(_connectionString);

        // Åbner forbindelsen
        connection.Open();

        // Starter en transaction fordi vi skal gemme i to tabeller
        // Hvis noget fejler bliver ændringerne fortrudt
        using SqlTransaction transaction = connection.BeginTransaction();

        // Forsøger at gemme produktet
        try
        {
            // SQL som gemmer de fælles produktoplysninger
            // OUTPUT INSERTED.Id henter det nye produkt id
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

            // Gør SQL klar og knytter den til vores transaction
            using SqlCommand productCommand = new SqlCommand(
                productSql, connection, transaction);

            // Sender produktets navn
            productCommand.Parameters.AddWithValue("@Name", bouquet.Name);

            // Sender beskrivelsen eller DBNull hvis den er tom
            productCommand.Parameters.AddWithValue(
                "@Description", (object?)bouquet.Description ?? DBNull.Value);

            // Sender produktets pris
            productCommand.Parameters.AddWithValue("@Price", bouquet.Price);

            // Sender billedets sti eller DBNull hvis den er tom
            productCommand.Parameters.AddWithValue(
                "@ImagePath", (object?)bouquet.ImagePath ?? DBNull.Value);

            // Sender billedets beskrivelse eller DBNull hvis den er tom
            productCommand.Parameters.AddWithValue(
                "@ImageDescription", (object?)bouquet.ImageDescription ?? DBNull.Value);

            // Sender om produktet er populært
            productCommand.Parameters.AddWithValue("@IsPopular", bouquet.IsPopular);

            // Sender hvor mange produkter der er på lager
            productCommand.Parameters.AddWithValue("@StockQuantity", bouquet.StockQuantity);

            // Gemmer produktet og henter det nye id fra databasen
            int productId = Convert.ToInt32(productCommand.ExecuteScalar());

            // SQL som gemmer de oplysninger der kun gælder buketter
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

            // Gør SQL klar og bruger den samme transaction
            using SqlCommand bouquetCommand = new SqlCommand(
                bouquetSql, connection, transaction);

            // Sender produktets nye id så tabellerne bliver forbundet
            bouquetCommand.Parameters.AddWithValue("@ProductId", productId);

            // Sender hvilken type blomster buketten indeholder
            bouquetCommand.Parameters.AddWithValue("@FlowerType", bouquet.FlowerType);

            // Laver BouquetSize om til tekst
            bouquetCommand.Parameters.AddWithValue("@BouquetSize", bouquet.BouquetSize.ToString());

            // Laver BookGenre om til tekst
            bouquetCommand.Parameters.AddWithValue("@BookGenre", bouquet.BookGenre.ToString());

            // Sender antallet af bøger i buketten
            bouquetCommand.Parameters.AddWithValue("@NumberOfBooks", bouquet.NumberOfBooks);

            // Gemmer bukettens oplysninger i databasen
            bouquetCommand.ExecuteNonQuery();

            // Godkender og gemmer begge ændringer samlet
            transaction.Commit();

            // Gemmer det nye id i vores Bouquet objekt
            bouquet.Id = productId;
        }
        // Fanger fejl hvis noget går galt
        catch
        {
            // Fortryder ændringerne hvis noget fejler
            transaction.Rollback();

            // Sender fejlen videre
            throw;
        }
    }

    // Opdaterer et eksisterende produkt
    public void Update(Product product)
    {
        // Kontrollerer at produktet er en Bouquet
        if (product is not Bouquet bouquet)
        {
            // Stopper hvis produktet ikke er en buket
            throw new ArgumentException("The product must be a bouquet.");
        }

        // Opretter forbindelsen til databasen
        using SqlConnection connection = new SqlConnection(_connectionString);

        // Åbner forbindelsen
        connection.Open();

        // Starter en transaction så begge tabeller opdateres samlet
        using SqlTransaction transaction = connection.BeginTransaction();

        // Forsøger at opdatere produktet
        try
        {
            // SQL som opdaterer de fælles produktoplysninger
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

            // Gør SQL klar og bruger den samme transaction
            using SqlCommand productCommand = new SqlCommand(
                productSql, connection, transaction);

            // Sender id på produktet som skal opdateres
            productCommand.Parameters.AddWithValue("@Id", bouquet.Id);

            // Sender det nye navn
            productCommand.Parameters.AddWithValue("@Name", bouquet.Name);

            // Sender beskrivelsen eller DBNull hvis den er null
            productCommand.Parameters.AddWithValue(
                "@Description", (object?)bouquet.Description ?? DBNull.Value);

            // Sender den nye pris
            productCommand.Parameters.AddWithValue("@Price", bouquet.Price);

            // Sender billedets sti eller DBNull hvis den er null
            productCommand.Parameters.AddWithValue(
                "@ImagePath", (object?)bouquet.ImagePath ?? DBNull.Value);

            // Sender billedets beskrivelse eller DBNull hvis den er null
            productCommand.Parameters.AddWithValue(
                "@ImageDescription", (object?)bouquet.ImageDescription ?? DBNull.Value);

            // Sender om produktet er populært
            productCommand.Parameters.AddWithValue("@IsPopular", bouquet.IsPopular);

            // Sender det nye antal på lager
            productCommand.Parameters.AddWithValue("@StockQuantity", bouquet.StockQuantity);

            // Opdaterer produktets oplysninger
            productCommand.ExecuteNonQuery();

            // SQL som opdaterer oplysningerne i Bouquets tabellen
            string bouquetSql = """
                UPDATE paperpetals.Bouquets
                SET
                    FlowerType = @FlowerType,
                    BouquetSize = @BouquetSize,
                    BookGenre = @BookGenre,
                    NumberOfBooks = @NumberOfBooks
                WHERE ProductId = @ProductId;
                """;

            // Gør SQL klar og bruger den samme transaction
            using SqlCommand bouquetCommand = new SqlCommand(
                bouquetSql, connection, transaction);

            // Sender id på buketten som skal opdateres
            bouquetCommand.Parameters.AddWithValue("@ProductId", bouquet.Id);

            // Sender den nye blomstertype
            bouquetCommand.Parameters.AddWithValue("@FlowerType", bouquet.FlowerType);

            // Sender bukettens størrelse som tekst
            bouquetCommand.Parameters.AddWithValue("@BouquetSize", bouquet.BouquetSize.ToString());

            // Sender boggenren som tekst
            bouquetCommand.Parameters.AddWithValue("@BookGenre", bouquet.BookGenre.ToString());

            // Sender antallet af bøger
            bouquetCommand.Parameters.AddWithValue("@NumberOfBooks", bouquet.NumberOfBooks);

            // Opdaterer oplysningerne om buketten
            bouquetCommand.ExecuteNonQuery();

            // Godkender og gemmer begge opdateringer
            transaction.Commit();
        }
        // Fanger fejl hvis noget går galt
        catch
        {
            // Fortryder begge opdateringer
            transaction.Rollback();

            // Sender fejlen videre
            throw;
        }
    }

    // Sletter et bestemt produkt fra databasen
    public void Delete(int id)
    {
        // Opretter forbindelsen til databasen
        using SqlConnection connection = new SqlConnection(_connectionString);

        // Åbner forbindelsen
        connection.Open();

        // Starter en transaction så begge sletninger hænger sammen
        using SqlTransaction transaction = connection.BeginTransaction();

        // Forsøger at slette produktet
        try
        {
            // Sletter først oplysningerne fra Bouquets
            // Det gør vi fordi Bouquets refererer til Products
            string bouquetSql = """
                DELETE FROM paperpetals.Bouquets
                WHERE ProductId = @ProductId;
                """;

            // Gør SQL klar og bruger vores transaction
            using SqlCommand bouquetCommand = new SqlCommand(
                bouquetSql, connection, transaction);

            // Sender id på buketten som skal slettes
            bouquetCommand.Parameters.AddWithValue("@ProductId", id);

            // Sletter bukettens oplysninger
            bouquetCommand.ExecuteNonQuery();

            // SQL som sletter produktet fra Products tabellen
            string productSql = """
                DELETE FROM paperpetals.Products
                WHERE Id = @Id;
                """;

            // Gør SQL klar og bruger den samme transaction
            using SqlCommand productCommand = new SqlCommand(
                productSql, connection, transaction);

            // Sender id på produktet som skal slettes
            productCommand.Parameters.AddWithValue("@Id", id);

            // Sletter produktet fra databasen
            productCommand.ExecuteNonQuery();

            // Godkender begge sletninger
            transaction.Commit();
        }
        // Fanger fejl hvis noget går galt
        catch
        {
            // Fortryder sletningerne hvis der opstår en fejl
            transaction.Rollback();

            // Sender fejlen videre
            throw;
        }
    }

    // Hjælpemetode som laver en række fra databasen om til en Bouquet
    private static Bouquet CreateBouquet(SqlDataReader reader)
    {
        // Opretter en ny Bouquet med oplysninger fra databasen
        return new Bouquet
        {
            // Henter produktets id fra første kolonne
            Id = reader.GetInt32(0),

            // Henter produktets navn fra anden kolonne
            Name = reader.GetString(1),

            // Tjekker om beskrivelsen er null
            // Returnerer null hvis den er tom ellers hentes teksten
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
