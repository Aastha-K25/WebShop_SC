using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Obligatoris.opgave.Domain.Interfaces;
using Obligatoris.opgave.Domain.Models;

namespace Obligatorisk_opgave.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    // Connection stringen bruges til at oprette forbindelse
    // til SQL Server-databasen.
    private readonly string _connectionString;


    // IConfiguration henter connection stringen fra User Secrets.
    public UserRepository(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("PaperPetalsDatabase")
            ?? throw new InvalidOperationException(
                "Database connection string was not found.");
    }


    // Henter alle brugere fra databasen.
    public List<User> GetAll()
    {
        List<User> users = new List<User>();

        string sql = """
            SELECT
                u.Id,
                u.Name,
                u.Email,
                u.PasswordHash,
                u.IsActive,
                c.Address,
                a.EmployeeNumber,
                a.MfaEnabled
            FROM paperpetals.Users u
            LEFT JOIN paperpetals.Customers c
                ON u.Id = c.UserId
            LEFT JOIN paperpetals.Admins a
                ON u.Id = a.UserId
            ORDER BY u.Name;
            """;

        using SqlConnection connection =
            new SqlConnection(_connectionString);

        using SqlCommand command =
            new SqlCommand(sql, connection);

        connection.Open();

        using SqlDataReader reader = command.ExecuteReader();

        // Løkken kører én gang for hver bruger,
        // som databasen returnerer.
        while (reader.Read())
        {
            User user = CreateUser(reader);

            users.Add(user);
        }

        return users;
    }


    // Finder én bruger ud fra brugerens id.
    public User? GetById(int id)
    {
        string sql = """
            SELECT
                u.Id,
                u.Name,
                u.Email,
                u.PasswordHash,
                u.IsActive,
                c.Address,
                a.EmployeeNumber,
                a.MfaEnabled
            FROM paperpetals.Users u
            LEFT JOIN paperpetals.Customers c
                ON u.Id = c.UserId
            LEFT JOIN paperpetals.Admins a
                ON u.Id = a.UserId
            WHERE u.Id = @Id;
            """;

        using SqlConnection connection =
            new SqlConnection(_connectionString);

        using SqlCommand command =
            new SqlCommand(sql, connection);

        // Id sendes som en SQL-parameter.
        // Det beskytter mod SQL Injection.
        command.Parameters.AddWithValue("@Id", id);

        connection.Open();

        using SqlDataReader reader = command.ExecuteReader();

        if (reader.Read())
        {
            return CreateUser(reader);
        }

        // Hvis brugeren ikke findes, returnerer vi null.
        return null;
    }


    // Finder en bruger ud fra e-mailadressen.
    // Metoden skal senere bruges ved login.
    public User? GetByEmail(string email)
    {
        string sql = """
            SELECT
                u.Id,
                u.Name,
                u.Email,
                u.PasswordHash,
                u.IsActive,
                c.Address,
                a.EmployeeNumber,
                a.MfaEnabled
            FROM paperpetals.Users u
            LEFT JOIN paperpetals.Customers c
                ON u.Id = c.UserId
            LEFT JOIN paperpetals.Admins a
                ON u.Id = a.UserId
            WHERE u.Email = @Email;
            """;

        using SqlConnection connection =
            new SqlConnection(_connectionString);

        using SqlCommand command =
            new SqlCommand(sql, connection);

        command.Parameters.AddWithValue(
            "@Email",
            email.Trim());

        connection.Open();

        using SqlDataReader reader = command.ExecuteReader();

        if (reader.Read())
        {
            return CreateUser(reader);
        }

        return null;
    }


    // Kontrollerer om e-mailadressen allerede findes.
    // Det bruges, når en kunde forsøger at oprette en konto.
    public bool EmailExists(string email)
    {
        string sql = """
            SELECT COUNT(1)
            FROM paperpetals.Users
            WHERE Email = @Email;
            """;

        using SqlConnection connection =
            new SqlConnection(_connectionString);

        using SqlCommand command =
            new SqlCommand(sql, connection);

        command.Parameters.AddWithValue(
            "@Email",
            email.Trim());

        connection.Open();

        int numberOfUsers =
            Convert.ToInt32(command.ExecuteScalar());

        // Hvis resultatet er større end 0, findes e-mailadressen allerede.
        return numberOfUsers > 0;
    }


    // Gemmer en ny bruger.
    public void Add(User user)
    {
        using SqlConnection connection =
            new SqlConnection(_connectionString);

        connection.Open();

        // Brugerens data skal gemmes i to tabeller.
        // En Customer gemmes i Users og Customers.
        // En Admin gemmes i Users og Admins.
        // En transaction sikrer, at begge handlinger lykkes. Hvis den ene fejler, bliver den anden rullet tilbage.
        using SqlTransaction transaction =
            connection.BeginTransaction();

        try
        {
            string userSql = """
                INSERT INTO paperpetals.Users
                (
                    Name,
                    Email,
                    PasswordHash,
                    IsActive
                )
                OUTPUT INSERTED.Id
                VALUES
                (
                    @Name,
                    @Email,
                    @PasswordHash,
                    @IsActive
                );
                """;

            using SqlCommand userCommand =
                new SqlCommand(
                    userSql,
                    connection,
                    transaction);

            userCommand.Parameters.AddWithValue(
                "@Name",
                user.Name.Trim());

            userCommand.Parameters.AddWithValue(
                "@Email",
                user.Email.Trim());

            // Vi gemmer kun passwordets hash. Et password må aldrig gemmes i klartekst.
            userCommand.Parameters.AddWithValue(
                "@PasswordHash",
                user.PasswordHash);

            userCommand.Parameters.AddWithValue(
                "@IsActive",
                user.IsActive);

            // SQL Server opretter brugerens id.
            int userId =
                Convert.ToInt32(userCommand.ExecuteScalar());


            // Hvis brugeren er en Customer, gemmes adressen i Customers-tabellen.
            if (user is Customer customer)
            {
                AddCustomer(
                    customer,
                    userId,
                    connection,
                    transaction);
            }
            // Hvis brugeren er en Admin, gemmes administratoroplysningerne i Admins-tabellen.
            else if (user is Admin admin)
            {
                AddAdmin(
                    admin,
                    userId,
                    connection,
                    transaction);
            }
            else
            {
                throw new ArgumentException(
                    "The user must be a customer or an admin.");
            }

            // Begge INSERT-statements lykkedes.
            transaction.Commit();

            // Det nye id gemmes også på User-objektet.
            user.Id = userId;
        }
        catch
        {
            // Hvis noget fejler, bliver begge ændringer fortrudt.
            transaction.Rollback();

            throw;
        }
    }


    // Opdaterer en eksisterende bruger.
    public void Update(User user)
    {
        using SqlConnection connection =
            new SqlConnection(_connectionString);

        connection.Open();

        // Både den fælles tabel og undertabellen skal opdateres samlet.
        using SqlTransaction transaction =
            connection.BeginTransaction();

        try
        {
            string userSql = """
                UPDATE paperpetals.Users
                SET
                    Name = @Name,
                    Email = @Email,
                    PasswordHash = @PasswordHash,
                    IsActive = @IsActive
                WHERE Id = @Id;
                """;

            using SqlCommand userCommand =
                new SqlCommand(
                    userSql,
                    connection,
                    transaction);

            userCommand.Parameters.AddWithValue(
                "@Id",
                user.Id);

            userCommand.Parameters.AddWithValue(
                "@Name",
                user.Name.Trim());

            userCommand.Parameters.AddWithValue(
                "@Email",
                user.Email.Trim());

            userCommand.Parameters.AddWithValue(
                "@PasswordHash",
                user.PasswordHash);

            userCommand.Parameters.AddWithValue(
                "@IsActive",
                user.IsActive);

            userCommand.ExecuteNonQuery();


            // Opdater Customer-oplysninger.
            if (user is Customer customer)
            {
                string customerSql = """
                    UPDATE paperpetals.Customers
                    SET Address = @Address
                    WHERE UserId = @UserId;
                    """;

                using SqlCommand customerCommand =
                    new SqlCommand(
                        customerSql,
                        connection,
                        transaction);

                customerCommand.Parameters.AddWithValue(
                    "@UserId",
                    customer.Id);

                customerCommand.Parameters.AddWithValue(
                    "@Address",
                    customer.Address);

                customerCommand.ExecuteNonQuery();
            }
            // Opdater Admin-oplysninger.
            else if (user is Admin admin)
            {
                string adminSql = """
                    UPDATE paperpetals.Admins
                    SET
                        EmployeeNumber = @EmployeeNumber,
                        MfaEnabled = @MfaEnabled
                    WHERE UserId = @UserId;
                    """;

                using SqlCommand adminCommand =
                    new SqlCommand(
                        adminSql,
                        connection,
                        transaction);

                adminCommand.Parameters.AddWithValue(
                    "@UserId",
                    admin.Id);

                adminCommand.Parameters.AddWithValue(
                    "@EmployeeNumber",
                    admin.EmployeeNumber);

                adminCommand.Parameters.AddWithValue(
                    "@MfaEnabled",
                    admin.MfaEnabled);

                adminCommand.ExecuteNonQuery();
            }
            else
            {
                throw new ArgumentException(
                    "The user must be a customer or an admin.");
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();

            throw;
        }
    }


    // Deaktiverer en bruger uden at slette brugerens data. Det er vigtigt, fordi brugerens gamle ordrer stadig skal kunne findes i databasen.
    public void Deactivate(int id)
    {
        string sql = """
            UPDATE paperpetals.Users
            SET IsActive = 0
            WHERE Id = @Id;
            """;

        using SqlConnection connection =
            new SqlConnection(_connectionString);

        using SqlCommand command =
            new SqlCommand(sql, connection);

        command.Parameters.AddWithValue("@Id", id);

        connection.Open();

        command.ExecuteNonQuery();
    }


    // Gemmer Customer-oplysningerne i Customers-tabellen.
    private static void AddCustomer(
        Customer customer,
        int userId,
        SqlConnection connection,
        SqlTransaction transaction)
    {
        string sql = """
            INSERT INTO paperpetals.Customers
            (
                UserId,
                Address
            )
            VALUES
            (
                @UserId,
                @Address
            );
            """;

        using SqlCommand command =
            new SqlCommand(
                sql,
                connection,
                transaction);

        command.Parameters.AddWithValue(
            "@UserId",
            userId);

        command.Parameters.AddWithValue(
            "@Address",
            customer.Address);

        command.ExecuteNonQuery();
    }


    // Gemmer Admin-oplysningerne i Admins-tabellen.
    private static void AddAdmin(
        Admin admin,
        int userId,
        SqlConnection connection,
        SqlTransaction transaction)
    {
        string sql = """
            INSERT INTO paperpetals.Admins
            (
                UserId,
                EmployeeNumber,
                MfaEnabled
            )
            VALUES
            (
                @UserId,
                @EmployeeNumber,
                @MfaEnabled
            );
            """;

        using SqlCommand command =
            new SqlCommand(
                sql,
                connection,
                transaction);

        command.Parameters.AddWithValue(
            "@UserId",
            userId);

        command.Parameters.AddWithValue(
            "@EmployeeNumber",
            admin.EmployeeNumber);

        command.Parameters.AddWithValue(
            "@MfaEnabled",
            admin.MfaEnabled);

        command.ExecuteNonQuery();
    }


    // Denne hjælpemetode omdanner én række fra databasen til enten et Customer- eller Admin-objekt.
    private static User CreateUser(
        SqlDataReader reader)
    {
        // Først læses oplysningerne fra Users-tabellen.
        int id = reader.GetInt32(0);
        string name = reader.GetString(1);
        string email = reader.GetString(2);
        string passwordHash = reader.GetString(3);
        bool isActive = reader.GetBoolean(4);


        // Address ligger på plads nummer 5. Hvis Address ikke er null, er brugeren en Customer.
        if (!reader.IsDBNull(5))
        {
            return new Customer
            {
                Id = id,
                Name = name,
                Email = email,
                PasswordHash = passwordHash,
                IsActive = isActive,
                Address = reader.GetString(5)
            };
        }


        // EmployeeNumber ligger på plads nummer 6. Hvis den ikke er null, er brugeren en Admin.
        if (!reader.IsDBNull(6))
        {
            return new Admin
            {
                Id = id,
                Name = name,
                Email = email,
                PasswordHash = passwordHash,
                IsActive = isActive,
                EmployeeNumber = reader.GetString(6),

                // MfaEnabled ligger på plads nummer 7.
                MfaEnabled = reader.GetBoolean(7)
            };
        }


        // En bruger skal findes i enten Customers eller Admins. Hvis ikke, er dataene i databasen ugyldige.
        throw new InvalidOperationException(
            "The user has no customer or admin information.");
    }
}