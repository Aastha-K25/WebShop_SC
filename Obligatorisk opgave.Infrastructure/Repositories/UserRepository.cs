
using Microsoft.Data.SqlClient; // Bruges til at arbejde med SQL Server
using Microsoft.Extensions.Configuration; // Bruges til at hente indstillinger
using Obligatoris.opgave.Domain.Interfaces; // Giver adgang til vores interfaces
using Obligatoris.opgave.Domain.Models; // Giver adgang til vores modeller

namespace Obligatorisk_opgave.Infrastructure.Repositories;

// Repository som håndterer brugere i databasen
public class UserRepository : IUserRepository
{
    // Gemmer forbindelsesoplysningerne til databasen
    private readonly string _connectionString;

    // Constructor som modtager vores konfiguration
    public UserRepository(IConfiguration configuration)
    {
        // Henter connection string fra konfigurationen
        // Stopper med en fejl hvis den ikke findes
        _connectionString = configuration.GetConnectionString("PaperPetalsDatabase")
            ?? throw new InvalidOperationException("Database connection string was not found.");
    }

    // Henter alle brugere fra databasen
    public List<User> GetAll()
    {
        // Opretter en tom liste til brugerne
        List<User> users = new List<User>();

        // Henter brugerne og deres oplysninger fra databasen
        // LEFT JOIN gør at vi også får brugere uden match i de andre tabeller
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

        // Opretter forbindelsen til databasen
        using SqlConnection connection = new SqlConnection(_connectionString);

        // Gør SQL forespørgslen klar
        using SqlCommand command = new SqlCommand(sql, connection);

        // Åbner forbindelsen
        connection.Open();

        // Kører SQL og læser resultaterne
        using SqlDataReader reader = command.ExecuteReader();

        // Går igennem alle brugerne som blev fundet
        while (reader.Read())
        {
            // Laver rækken fra databasen om til et User objekt
            User user = CreateUser(reader);

            // Tilføjer brugeren til listen
            users.Add(user);
        }

        // Returnerer listen med alle brugere
        return users;
    }

    // Finder en bestemt bruger ud fra dens id
    public User? GetById(int id)
    {
        // SQL som finder brugeren og dens oplysninger
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

        // Opretter forbindelsen til databasen
        using SqlConnection connection = new SqlConnection(_connectionString);

        // Gør SQL forespørgslen klar
        using SqlCommand command = new SqlCommand(sql, connection);

        // Sender brugerens id som parameter for at undgå SQL injection
        command.Parameters.AddWithValue("@Id", id);

        // Åbner forbindelsen
        connection.Open();

        // Kører SQL og læser resultatet
        using SqlDataReader reader = command.ExecuteReader();

        // Kontrollerer om brugeren blev fundet
        if (reader.Read())
        {
            // Laver resultatet om til et User objekt
            return CreateUser(reader);
        }

        // Returnerer null hvis brugeren ikke findes
        return null;
    }

    // Finder en bruger ud fra dens email
    public User? GetByEmail(string email)
    {
        // Fjerner mellemrum og laver email om til små bogstaver
        string normalizedEmail = email.Trim().ToLowerInvariant();

        // SQL som finder brugeren ved at sammenligne email
        // LOWER gør at store og små bogstaver behandles ens
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

        // Opretter forbindelsen til databasen
        using SqlConnection connection = new SqlConnection(_connectionString);

        // Gør SQL forespørgslen klar
        using SqlCommand command = new SqlCommand(sql, connection);

        // Sender den normaliserede email som parameter
        command.Parameters.AddWithValue("@Email", normalizedEmail);

        // Åbner forbindelsen
        connection.Open();

        // Kører SQL og læser resultatet
        using SqlDataReader reader = command.ExecuteReader();

        // Kontrollerer om brugeren blev fundet
        if (reader.Read())
        {
            // Laver resultatet om til et User objekt
            return CreateUser(reader);
        }

        // Returnerer null hvis email ikke findes
        return null;
    }

    // Kontrollerer om en email allerede findes i databasen
    public bool EmailExists(string email)
    {
        // Fjerner mellemrum og laver email om til små bogstaver
        string normalizedEmail = email.Trim().ToLowerInvariant();

        // SQL som tæller hvor mange brugere der har denne email
        string sql = """
            SELECT COUNT(1)
            FROM paperpetals.Users
            WHERE LOWER(Email) = @Email;
            """;

        // Opretter forbindelsen til databasen
        using SqlConnection connection = new SqlConnection(_connectionString);

        // Gør SQL forespørgslen klar
        using SqlCommand command = new SqlCommand(sql, connection);

        // Sender email som parameter
        command.Parameters.AddWithValue("@Email", normalizedEmail);

        // Åbner forbindelsen
        connection.Open();

        // Henter antallet af brugere med denne email
        int numberOfUsers = Convert.ToInt32(command.ExecuteScalar());

        // Returnerer true hvis email allerede findes
        // Ellers returnerer den false
        return numberOfUsers > 0;
    }

    // Gemmer en ny bruger i databasen
    public void Add(User user)
    {
        // Opretter forbindelsen til databasen
        using SqlConnection connection = new SqlConnection(_connectionString);

        // Åbner forbindelsen
        connection.Open();

        // Starter en transaction så ændringerne gemmes samlet
        using SqlTransaction transaction = connection.BeginTransaction();

        // Forsøger at oprette brugeren
        try
        {
            // SQL som gemmer de fælles brugeroplysninger
            // OUTPUT INSERTED.Id henter det nye bruger id
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

            // Gør SQL klar og knytter den til vores transaction
            using SqlCommand userCommand = new SqlCommand(
                userSql, connection, transaction);

            // Sender brugerens navn uden ekstra mellemrum
            userCommand.Parameters.AddWithValue("@Name", user.Name.Trim());

            // Sender brugerens efternavn uden ekstra mellemrum
            userCommand.Parameters.AddWithValue("@Surname", user.Surname.Trim());

            // Gemmer email uden ekstra mellemrum og med små bogstaver
            userCommand.Parameters.AddWithValue("@Email", user.Email.Trim().ToLowerInvariant());

            // Gemmer kun passwordets hash og ikke selve passwordet
            userCommand.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);

            // Sender om brugeren er aktiv
            userCommand.Parameters.AddWithValue("@IsActive", user.IsActive);

            // Opretter brugeren og henter dens nye id
            int userId = Convert.ToInt32(userCommand.ExecuteScalar());

            // Kontrollerer om brugeren er en Customer
            if (user is Customer customer)
            {
                // Gemmer oplysningerne i Customers tabellen
                AddCustomer(customer, userId, connection, transaction);
            }
            // Kontrollerer om brugeren er en Admin
            else if (user is Admin admin)
            {
                // Gemmer oplysningerne i Admins tabellen
                AddAdmin(admin, userId, connection, transaction);
            }
            else
            {
                // Stopper hvis brugeren hverken er Customer eller Admin
                throw new ArgumentException("The user must be a customer or an admin.");
            }

            // Godkender og gemmer alle ændringer samlet
            transaction.Commit();

            // Gemmer det nye id i vores User objekt
            user.Id = userId;
        }
        // Fanger fejl hvis noget går galt
        catch
        {
            // Fortryder ændringerne
            transaction.Rollback();

            // Sender fejlen videre
            throw;
        }
    }

    // Opdaterer en eksisterende bruger
    public void Update(User user)
    {
        // Opretter forbindelsen til databasen
        using SqlConnection connection = new SqlConnection(_connectionString);

        // Åbner forbindelsen
        connection.Open();

        // Starter en transaction så opdateringerne hænger sammen
        using SqlTransaction transaction = connection.BeginTransaction();

        // Forsøger at opdatere brugeren
        try
        {
            // SQL som opdaterer de fælles brugeroplysninger
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

            // Gør SQL klar og bruger vores transaction
            using SqlCommand userCommand = new SqlCommand(
                userSql, connection, transaction);

            // Sender id på brugeren som skal opdateres
            userCommand.Parameters.AddWithValue("@Id", user.Id);

            // Sender det nye navn uden ekstra mellemrum
            userCommand.Parameters.AddWithValue("@Name", user.Name.Trim());

            // Sender det nye efternavn uden ekstra mellemrum
            userCommand.Parameters.AddWithValue("@Surname", user.Surname.Trim());

            // Sender email med små bogstaver og uden ekstra mellemrum
            userCommand.Parameters.AddWithValue("@Email", user.Email.Trim().ToLowerInvariant());

            // Sender passwordets hash
            userCommand.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);

            // Sender om brugeren er aktiv
            userCommand.Parameters.AddWithValue("@IsActive", user.IsActive);

            // Opdaterer brugerens fælles oplysninger
            userCommand.ExecuteNonQuery();

            // Kontrollerer om brugeren er en Customer
            if (user is Customer customer)
            {
                // SQL som opdaterer kundens adresse
                string customerSql = """
                    UPDATE paperpetals.Customers
                    SET Address = @Address
                    WHERE UserId = @UserId;
                    """;

                // Gør SQL klar og bruger den samme transaction
                using SqlCommand customerCommand = new SqlCommand(
                    customerSql, connection, transaction);

                // Sender kundens id
                customerCommand.Parameters.AddWithValue("@UserId", customer.Id);

                // Sender adressen uden ekstra mellemrum
                customerCommand.Parameters.AddWithValue("@Address", customer.Address.Trim());

                // Opdaterer kundens adresse i databasen
                customerCommand.ExecuteNonQuery();
            }
            // Kontrollerer om brugeren er en Admin
            else if (user is Admin admin)
            {
                // SQL som opdaterer oplysningerne om admin
                string adminSql = """
                    UPDATE paperpetals.Admins
                    SET
                        EmployeeNumber = @EmployeeNumber,
                        MfaEnabled = @MfaEnabled
                    WHERE UserId = @UserId;
                    """;

                // Gør SQL klar og bruger den samme transaction
                using SqlCommand adminCommand = new SqlCommand(
                    adminSql, connection, transaction);

                // Sender id på admin
                adminCommand.Parameters.AddWithValue("@UserId", admin.Id);

                // Sender medarbejdernummeret
                adminCommand.Parameters.AddWithValue("@EmployeeNumber", admin.EmployeeNumber);

                // Sender om MFA er aktiveret
                adminCommand.Parameters.AddWithValue("@MfaEnabled", admin.MfaEnabled);

                // Opdaterer oplysningerne om admin
                adminCommand.ExecuteNonQuery();
            }
            else
            {
                // Stopper hvis brugeren hverken er Customer eller Admin
                throw new ArgumentException("The user must be a customer or an admin.");
            }

            // Godkender og gemmer opdateringerne samlet
            transaction.Commit();
        }
        // Fanger fejl hvis noget går galt
        catch
        {
            // Fortryder alle ændringerne
            transaction.Rollback();

            // Sender fejlen videre
            throw;
        }
    }

    // Deaktiverer en bruger uden at slette oplysningerne
    public void Deactivate(int id)
    {
        // SQL som ændrer IsActive til false
        // Brugeren bliver gemt så gamle ordrer stadig kan findes
        string sql = """
            UPDATE paperpetals.Users
            SET IsActive = 0
            WHERE Id = @Id;
            """;

        // Opretter forbindelsen til databasen
        using SqlConnection connection = new SqlConnection(_connectionString);

        // Gør SQL forespørgslen klar
        using SqlCommand command = new SqlCommand(sql, connection);

        // Sender id på brugeren som skal deaktiveres
        command.Parameters.AddWithValue("@Id", id);

        // Åbner forbindelsen
        connection.Open();

        // Kører SQL og deaktiverer brugeren
        command.ExecuteNonQuery();
    }

    // Hjælpemetode som gemmer en ny Customer i databasen
    private static void AddCustomer(
        Customer customer,
        int userId,
        SqlConnection connection,
        SqlTransaction transaction)
    {
        // SQL som gemmer kundens id og adresse
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

        // Gør SQL klar og bruger den samme transaction
        using SqlCommand command = new SqlCommand(sql, connection, transaction);

        // Sender brugerens id så Customer forbindes med User
        command.Parameters.AddWithValue("@UserId", userId);

        // Sender kundens adresse uden ekstra mellemrum
        command.Parameters.AddWithValue("@Address", customer.Address.Trim());

        // Gemmer kunden i Customers tabellen
        command.ExecuteNonQuery();
    }

    // Hjælpemetode som gemmer en ny Admin i databasen
    private static void AddAdmin(
        Admin admin,
        int userId,
        SqlConnection connection,
        SqlTransaction transaction)
    {
        // SQL som gemmer oplysningerne om admin
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

        // Gør SQL klar og bruger den samme transaction
        using SqlCommand command = new SqlCommand(sql, connection, transaction);

        // Sender brugerens id så Admin forbindes med User
        command.Parameters.AddWithValue("@UserId", userId);

        // Sender medarbejdernummeret
        command.Parameters.AddWithValue("@EmployeeNumber", admin.EmployeeNumber);

        // Sender om MFA er aktiveret
        command.Parameters.AddWithValue("@MfaEnabled", admin.MfaEnabled);

        // Gemmer admin i Admins tabellen
        command.ExecuteNonQuery();
    }

    // Hjælpemetode som laver data fra databasen om til en User
    // Finder ud af om brugeren er en Customer eller Admin
    private static User CreateUser(SqlDataReader reader)
    {
        // Henter brugerens id fra første kolonne
        int id = reader.GetInt32(0);

        // Henter brugerens navn fra anden kolonne
        string name = reader.GetString(1);

        // Henter brugerens efternavn fra tredje kolonne
        string surname = reader.GetString(2);

        // Henter brugerens email fra fjerde kolonne
        string email = reader.GetString(3);

        // Henter passwordets hash fra femte kolonne
        string passwordHash = reader.GetString(4);

        // Henter om brugeren er aktiv fra sjette kolonne
        bool isActive = reader.GetBoolean(5);

        // Kontrollerer om Address indeholder en værdi
        // Hvis den gør er brugeren en Customer
        if (!reader.IsDBNull(6))
        {
            // Opretter et Customer objekt
            return new Customer
            {
                // Gemmer brugerens id
                Id = id,

                // Gemmer brugerens navn
                Name = name,

                // Gemmer brugerens efternavn
                Surname = surname,

                // Gemmer brugerens email
                Email = email,

                // Gemmer passwordets hash
                PasswordHash = passwordHash,

                // Gemmer om brugeren er aktiv
                IsActive = isActive,

                // Henter kundens adresse fra databasen
                Address = reader.GetString(6)
            };
        }

        // Kontrollerer om EmployeeNumber indeholder en værdi
        // Hvis den gør er brugeren en Admin
        if (!reader.IsDBNull(7))
        {
            // Opretter et Admin objekt
            return new Admin
            {
                // Gemmer brugerens id
                Id = id,

                // Gemmer brugerens navn
                Name = name,

                // Gemmer brugerens efternavn
                Surname = surname,

                // Gemmer brugerens email
                Email = email,

                // Gemmer passwordets hash
                PasswordHash = passwordHash,

                // Gemmer om brugeren er aktiv
                IsActive = isActive,

                // Henter medarbejdernummeret
                EmployeeNumber = reader.GetString(7),

                // Henter om MFA er aktiveret
                MfaEnabled = reader.GetBoolean(8)
            };
        }

        // Stopper hvis brugeren hverken har Customer eller Admin oplysninger
        throw new InvalidOperationException("The user has no customer or admin information.");
    }
}
