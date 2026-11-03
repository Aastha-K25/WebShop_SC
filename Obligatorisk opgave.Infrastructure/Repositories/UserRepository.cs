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
                u.Surname,
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
            ORDER BY u.Name, u.Surname;
            """;

        using SqlConnection connection =
            new SqlConnection(_connectionString);

        using SqlCommand command =
            new SqlCommand(sql, connection);

        connection.Open();

        using SqlDataReader reader =
            command.ExecuteReader();


        while (reader.Read())
        {
            User user =
                CreateUser(reader);

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
                u.Surname,
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


        command.Parameters.AddWithValue(
            "@Id",
            id);


        connection.Open();

        using SqlDataReader reader =
            command.ExecuteReader();


        if (reader.Read())
        {
            return CreateUser(reader);
        }


        return null;
    }


    // Finder en bruger ud fra e-mailadressen.
    public User? GetByEmail(string email)
    {
        string normalizedEmail =
            email
                .Trim()
                .ToLowerInvariant();


        string sql = """
            SELECT
                u.Id,
                u.Name,
                u.Surname,
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
            WHERE LOWER(u.Email) = @Email;
            """;

        using SqlConnection connection =
            new SqlConnection(_connectionString);

        using SqlCommand command =
            new SqlCommand(sql, connection);


        command.Parameters.AddWithValue(
            "@Email",
            normalizedEmail);


        connection.Open();

        using SqlDataReader reader =
            command.ExecuteReader();


        if (reader.Read())
        {
            return CreateUser(reader);
        }


        return null;
    }


    // Kontrollerer om e-mailadressen allerede findes.
    public bool EmailExists(string email)
    {
        string normalizedEmail =
            email
                .Trim()
                .ToLowerInvariant();


        string sql = """
            SELECT COUNT(1)
            FROM paperpetals.Users
            WHERE LOWER(Email) = @Email;
            """;

        using SqlConnection connection =
            new SqlConnection(_connectionString);

        using SqlCommand command =
            new SqlCommand(sql, connection);


        command.Parameters.AddWithValue(
            "@Email",
            normalizedEmail);


        connection.Open();


        int numberOfUsers =
            Convert.ToInt32(
                command.ExecuteScalar());


        return numberOfUsers > 0;
    }


    // Gemmer en ny bruger.
    public void Add(User user)
    {
        using SqlConnection connection =
            new SqlConnection(_connectionString);

        connection.Open();


        using SqlTransaction transaction =
            connection.BeginTransaction();


        try
        {
            string userSql = """
                INSERT INTO paperpetals.Users
                (
                    Name,
                    Surname,
                    Email,
                    PasswordHash,
                    IsActive
                )
                OUTPUT INSERTED.Id
                VALUES
                (
                    @Name,
                    @Surname,
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
                "@Surname",
                user.Surname.Trim());


            userCommand.Parameters.AddWithValue(
                "@Email",
                user.Email
                    .Trim()
                    .ToLowerInvariant());


            // Vi gemmer kun passwordets hash.
            // Password må aldrig gemmes i klartekst.
            userCommand.Parameters.AddWithValue(
                "@PasswordHash",
                user.PasswordHash);


            userCommand.Parameters.AddWithValue(
                "@IsActive",
                user.IsActive);


            int userId =
                Convert.ToInt32(
                    userCommand.ExecuteScalar());


            if (user is Customer customer)
            {
                AddCustomer(
                    customer,
                    userId,
                    connection,
                    transaction);
            }
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


            transaction.Commit();

            user.Id = userId;
        }
        catch
        {
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


        using SqlTransaction transaction =
            connection.BeginTransaction();


        try
        {
            string userSql = """
                UPDATE paperpetals.Users
                SET
                    Name = @Name,
                    Surname = @Surname,
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
                "@Surname",
                user.Surname.Trim());


            userCommand.Parameters.AddWithValue(
                "@Email",
                user.Email
                    .Trim()
                    .ToLowerInvariant());


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
                    customer.Address.Trim());


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


    // Deaktiverer en bruger uden at slette data.
    // Gamle ordrer skal stadig kunne findes.
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
            new SqlCommand(
                sql,
                connection);


        command.Parameters.AddWithValue(
            "@Id",
            id);


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
            customer.Address.Trim());


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


    // Omdanner én række fra databasen
    // til enten Customer eller Admin.
    private static User CreateUser(
        SqlDataReader reader)
    {
        int id =
            reader.GetInt32(0);

        string name =
            reader.GetString(1);

        string surname =
            reader.GetString(2);

        string email =
            reader.GetString(3);

        string passwordHash =
            reader.GetString(4);

        bool isActive =
            reader.GetBoolean(5);


        // Address ligger på plads nummer 6.
        // Hvis Address ikke er null, er brugeren Customer.
        if (!reader.IsDBNull(6))
        {
            return new Customer
            {
                Id = id,
                Name = name,
                Surname = surname,
                Email = email,
                PasswordHash = passwordHash,
                IsActive = isActive,
                Address = reader.GetString(6)
            };
        }


        // EmployeeNumber ligger på plads nummer 7.
        // Hvis den ikke er null, er brugeren Admin.
        if (!reader.IsDBNull(7))
        {
            return new Admin
            {
                Id = id,
                Name = name,
                Surname = surname,
                Email = email,
                PasswordHash = passwordHash,
                IsActive = isActive,
                EmployeeNumber = reader.GetString(7),

                // MfaEnabled ligger på plads nummer 8.
                MfaEnabled =
                    reader.GetBoolean(8)
            };
        }


        throw new InvalidOperationException(
            "The user has no customer or admin information.");
    }
}