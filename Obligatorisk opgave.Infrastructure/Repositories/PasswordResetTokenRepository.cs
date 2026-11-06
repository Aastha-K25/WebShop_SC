
using Microsoft.Data.SqlClient; // Bruges til at arbejde med SQL Server
using Microsoft.Extensions.Configuration; // Bruges til at hente indstillinger
using Obligatoris.opgave.Domain.Interfaces; // Giver adgang til vores interfaces
using Obligatoris.opgave.Domain.Models; // Giver adgang til vores modeller

namespace Obligatorisk_opgave.Infrastructure.Repositories;

// Repository som håndterer password reset tokens i databasen
public class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    // Gemmer forbindelsesoplysningerne til databasen
    private readonly string _connectionString;

    // Constructor som modtager vores konfiguration
    public PasswordResetTokenRepository(IConfiguration configuration)
    {
        // Henter connection string fra konfigurationen
        // Stopper med en fejl hvis den ikke findes
        _connectionString = configuration.GetConnectionString("PaperPetalsDatabase")
            ?? throw new InvalidOperationException("Database connection string was not found.");
    }

    // Finder et password reset token ud fra dets hash
    public PasswordResetToken? GetByTokenHash(string tokenHash)
    {
        // SQL som finder tokenet ud fra dets hash
        // Vi søger efter hash fordi det rigtige token ikke gemmes i databasen
        string sql = """
            SELECT
                Id,
                UserId,
                TokenHash,
                ExpiresAt,
                UsedAt
            FROM paperpetals.PasswordResetTokens
            WHERE TokenHash = @TokenHash;
            """;

        // Opretter forbindelsen til databasen
        using SqlConnection connection = new SqlConnection(_connectionString);

        // Gør SQL forespørgslen klar
        using SqlCommand command = new SqlCommand(sql, connection);

        // Sender tokenets hash som parameter for at undgå SQL injection
        command.Parameters.AddWithValue("@TokenHash", tokenHash);

        // Åbner forbindelsen til databasen
        connection.Open();

        // Kører SQL og læser resultatet
        using SqlDataReader reader = command.ExecuteReader();

        // Tjekker om databasen fandt et token
        if (reader.Read())
        {
            // Laver resultatet om til et PasswordResetToken objekt
            return CreatePasswordResetToken(reader);
        }

        // Returnerer null hvis tokenet ikke findes
        return null;
    }

    // Gemmer et nyt password reset token i databasen
    public void Add(PasswordResetToken token)
    {
        // SQL som indsætter et nyt token
        // OUTPUT INSERTED.Id henter det nye id
        string sql = """
            INSERT INTO paperpetals.PasswordResetTokens
            (
                UserId,
                TokenHash,
                ExpiresAt,
                UsedAt
            )
            OUTPUT INSERTED.Id
            VALUES
            (
                @UserId,
                @TokenHash,
                @ExpiresAt,
                @UsedAt
            );
            """;

        // Opretter forbindelsen til databasen
        using SqlConnection connection = new SqlConnection(_connectionString);

        // Gør SQL forespørgslen klar
        using SqlCommand command = new SqlCommand(sql, connection);

        // Sender id på brugeren som ejer tokenet
        command.Parameters.AddWithValue("@UserId", token.UserId);

        // Sender tokenets hash i stedet for selve tokenet
        command.Parameters.AddWithValue("@TokenHash", token.TokenHash);

        // Sender tidspunktet hvor tokenet udløber
        command.Parameters.AddWithValue("@ExpiresAt", token.ExpiresAt);

        // Sender tidspunktet hvor tokenet blev brugt
        // Bruger DBNull.Value hvis UsedAt er null
        command.Parameters.AddWithValue("@UsedAt", (object?)token.UsedAt ?? DBNull.Value);

        // Åbner forbindelsen til databasen
        connection.Open();

        // Opretter tokenet og henter det nye id
        int newTokenId = Convert.ToInt32(command.ExecuteScalar());

        // Gemmer det nye id i vores token objekt
        token.Id = newTokenId;
    }

    // Opdaterer et eksisterende password reset token
    public void Update(PasswordResetToken token)
    {
        // SQL som opdaterer tokenets oplysninger
        // Bruges blandt andet til at gemme hvornår tokenet blev brugt
        string sql = """
            UPDATE paperpetals.PasswordResetTokens
            SET
                UserId = @UserId,
                TokenHash = @TokenHash,
                ExpiresAt = @ExpiresAt,
                UsedAt = @UsedAt
            WHERE Id = @Id;
            """;

        // Opretter forbindelsen til databasen
        using SqlConnection connection = new SqlConnection(_connectionString);

        // Gør SQL forespørgslen klar
        using SqlCommand command = new SqlCommand(sql, connection);

        // Sender id på tokenet som skal opdateres
        command.Parameters.AddWithValue("@Id", token.Id);

        // Sender brugerens id
        command.Parameters.AddWithValue("@UserId", token.UserId);

        // Sender tokenets hash
        command.Parameters.AddWithValue("@TokenHash", token.TokenHash);

        // Sender tokenets udløbstidspunkt
        command.Parameters.AddWithValue("@ExpiresAt", token.ExpiresAt);

        // Sender tidspunktet hvor tokenet blev brugt
        // Bruger DBNull.Value hvis det endnu ikke er brugt
        command.Parameters.AddWithValue("@UsedAt", (object?)token.UsedAt ?? DBNull.Value);

        // Åbner forbindelsen til databasen
        connection.Open();

        // Kører SQL og opdaterer tokenet
        command.ExecuteNonQuery();
    }

    // Sletter tokens som er udløbet eller allerede brugt
    public void DeleteExpiredTokens()
    {
        // SQL som sletter tokens der ikke længere kan bruges
        string sql = """
            DELETE FROM paperpetals.PasswordResetTokens
            WHERE ExpiresAt <= @CurrentTime
               OR UsedAt IS NOT NULL;
            """;

        // Opretter forbindelsen til databasen
        using SqlConnection connection = new SqlConnection(_connectionString);

        // Gør SQL forespørgslen klar
        using SqlCommand command = new SqlCommand(sql, connection);

        // Sender det aktuelle tidspunkt i UTC
        // Bruges til at finde tokens som er udløbet
        command.Parameters.AddWithValue("@CurrentTime", DateTime.UtcNow);

        // Åbner forbindelsen til databasen
        connection.Open();

        // Kører SQL og sletter de ugyldige tokens
        command.ExecuteNonQuery();
    }

    // Hjælpemetode som laver data fra databasen om til et token objekt
    private static PasswordResetToken CreatePasswordResetToken(SqlDataReader reader)
    {
        // Opretter et PasswordResetToken objekt med værdier fra databasen
        return new PasswordResetToken
        {
            // Henter tokenets id fra første kolonne
            Id = reader.GetInt32(0),

            // Henter brugerens id fra anden kolonne
            UserId = reader.GetInt32(1),

            // Henter tokenets hash fra tredje kolonne
            TokenHash = reader.GetString(2),

            // Henter udløbstidspunktet fra fjerde kolonne
            ExpiresAt = reader.GetDateTime(3),

            // Tjekker om UsedAt er tom i databasen
            // Returnerer null hvis det ikke er brugt
            // Ellers henter den tidspunktet hvor det blev brugt
            UsedAt = reader.IsDBNull(4)
                ? null
                : reader.GetDateTime(4)
        };
    }
}
