using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Obligatoris.opgave.Domain.Interfaces;
using Obligatoris.opgave.Domain.Models;

namespace Obligatorisk_opgave.Infrastructure.Repositories;

public class PasswordResetTokenRepository
    : IPasswordResetTokenRepository
{
    // Connection stringen bruges til at oprette forbindelse
    // til SQL Server-databasen.
    private readonly string _connectionString;


    // Connection stringen hentes fra User Secrets.
    public PasswordResetTokenRepository(
        IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("PaperPetalsDatabase")
            ?? throw new InvalidOperationException(
                "Database connection string was not found.");
    }


    // Finder et password reset-token ud fra tokenets hash.
    //
    // Vi gemmer ikke det oprindelige token i databasen.
    // Derfor søger vi efter den sikre hash-værdi.
    public PasswordResetToken? GetByTokenHash(
        string tokenHash)
    {
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

        using SqlConnection connection =
            new SqlConnection(_connectionString);

        using SqlCommand command =
            new SqlCommand(sql, connection);

        // Tokenets hash sendes som en SQL-parameter.
        command.Parameters.AddWithValue(
            "@TokenHash",
            tokenHash);

        connection.Open();

        using SqlDataReader reader = command.ExecuteReader();

        if (reader.Read())
        {
            return CreatePasswordResetToken(reader);
        }

        // Hvis tokenet ikke findes, returnerer vi null.
        return null;
    }


    // Gemmer et nyt password reset-token.
    public void Add(PasswordResetToken token)
    {
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

        using SqlConnection connection =
            new SqlConnection(_connectionString);

        using SqlCommand command =
            new SqlCommand(sql, connection);

        command.Parameters.AddWithValue(
            "@UserId",
            token.UserId);

        command.Parameters.AddWithValue(
            "@TokenHash",
            token.TokenHash);

        command.Parameters.AddWithValue(
            "@ExpiresAt",
            token.ExpiresAt);

        // Et nyt token er endnu ikke blevet brugt.
        // UsedAt er derfor normalt null.
        command.Parameters.AddWithValue(
            "@UsedAt",
            (object?)token.UsedAt ?? DBNull.Value);

        connection.Open();

        // SQL Server opretter tokenets id.
        int newTokenId =
            Convert.ToInt32(command.ExecuteScalar());

        // Det nye id gemmes også på objektet.
        token.Id = newTokenId;
    }


    // Opdaterer et eksisterende token.
    //
    // Metoden bruges blandt andet, når tokenet er blevet anvendt,
    // og UsedAt skal gemmes i databasen.
    public void Update(PasswordResetToken token)
    {
        string sql = """
            UPDATE paperpetals.PasswordResetTokens
            SET
                UserId = @UserId,
                TokenHash = @TokenHash,
                ExpiresAt = @ExpiresAt,
                UsedAt = @UsedAt
            WHERE Id = @Id;
            """;

        using SqlConnection connection =
            new SqlConnection(_connectionString);

        using SqlCommand command =
            new SqlCommand(sql, connection);

        command.Parameters.AddWithValue(
            "@Id",
            token.Id);

        command.Parameters.AddWithValue(
            "@UserId",
            token.UserId);

        command.Parameters.AddWithValue(
            "@TokenHash",
            token.TokenHash);

        command.Parameters.AddWithValue(
            "@ExpiresAt",
            token.ExpiresAt);

        command.Parameters.AddWithValue(
            "@UsedAt",
            (object?)token.UsedAt ?? DBNull.Value);

        connection.Open();

        command.ExecuteNonQuery();
    }


    // Sletter tokens, som er udløbet.
    //
    // Det begrænser mængden af gamle og sikkerhedsfølsomme
    // oplysninger i databasen.
    public void DeleteExpiredTokens()
    {
        string sql = """
            DELETE FROM paperpetals.PasswordResetTokens
            WHERE ExpiresAt <= @CurrentTime;
            """;

        using SqlConnection connection =
            new SqlConnection(_connectionString);

        using SqlCommand command =
            new SqlCommand(sql, connection);

        // Vi bruger UTC, fordi ExpiresAt også gemmes som UTC.
        command.Parameters.AddWithValue(
            "@CurrentTime",
            DateTime.UtcNow);

        connection.Open();

        command.ExecuteNonQuery();
    }


    // Denne hjælpemetode laver én række fra databasen
    // om til et PasswordResetToken-objekt.
    private static PasswordResetToken CreatePasswordResetToken(
        SqlDataReader reader)
    {
        return new PasswordResetToken
        {
            Id = reader.GetInt32(0),

            UserId = reader.GetInt32(1),

            TokenHash = reader.GetString(2),

            ExpiresAt = reader.GetDateTime(3),

            // UsedAt må gerne være null,
            // hvis tokenet endnu ikke er blevet brugt.
            UsedAt = reader.IsDBNull(4)
                ? null
                : reader.GetDateTime(4)
        };
    }
}