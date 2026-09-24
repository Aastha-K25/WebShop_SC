using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Obligatoris.opgave.Domain.Interfaces;
using Obligatoris.opgave.Domain.Models;

namespace Obligatorisk_opgave.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    // Connection stringen bruges til at oprette forbindelse
    // til SQL Server-databasen.
    private readonly string _connectionString;


    // Connection stringen hentes fra User Secrets.
    public OrderRepository(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("PaperPetalsDatabase")
            ?? throw new InvalidOperationException(
                "Database connection string was not found.");
    }


    // Henter én ordre ud fra ordrens id.
    public Order? GetById(int id)
    {
        string sql = """
            SELECT
                Id,
                CustomerId,
                OrderDate,
                TotalPrice,
                Status
            FROM paperpetals.Orders
            WHERE Id = @Id;
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
            return CreateOrder(reader);
        }

        // Hvis ordren ikke findes, returnerer vi null.
        return null;
    }


    // Henter alle ordrer, som tilhører en bestemt kunde.
    //
    // CustomerId skal senere komme fra den bruger,
    // som er logget ind. Kunden må ikke selv bestemme id'et.
    public List<Order> GetByCustomerId(int customerId)
    {
        List<Order> orders = new List<Order>();

        string sql = """
            SELECT
                Id,
                CustomerId,
                OrderDate,
                TotalPrice,
                Status
            FROM paperpetals.Orders
            WHERE CustomerId = @CustomerId
            ORDER BY OrderDate DESC;
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

        // Løkken kører én gang for hver ordre,
        // som tilhører kunden.
        while (reader.Read())
        {
            Order order = CreateOrder(reader);

            orders.Add(order);
        }

        return orders;
    }


    // Gemmer en ny ordre og returnerer ordrens nye id.
    public int Add(Order order)
    {
        string sql = """
            INSERT INTO paperpetals.Orders
            (
                CustomerId,
                OrderDate,
                TotalPrice,
                Status
            )
            OUTPUT INSERTED.Id
            VALUES
            (
                @CustomerId,
                @OrderDate,
                @TotalPrice,
                @Status
            );
            """;

        using SqlConnection connection =
            new SqlConnection(_connectionString);

        using SqlCommand command =
            new SqlCommand(sql, connection);

        command.Parameters.AddWithValue(
            "@CustomerId",
            order.CustomerId);

        command.Parameters.AddWithValue(
            "@OrderDate",
            order.OrderDate);

        // TotalPrice skal beregnes i backend.
        // Kunden må aldrig selv indsende den samlede pris.
        command.Parameters.AddWithValue(
            "@TotalPrice",
            order.TotalPrice);

        // Enum-værdien gemmes som tekst.
        // Eksempel: Pending, Paid eller Shipped.
        command.Parameters.AddWithValue(
            "@Status",
            order.Status.ToString());

        connection.Open();

        // SQL Server opretter ordrens id.
        int newOrderId =
            Convert.ToInt32(command.ExecuteScalar());

        // Det nye id gemmes også på Order-objektet.
        order.Id = newOrderId;

        return newOrderId;
    }


    // Gemmer forbindelsen mellem en ordre og et produkt.
    //
    // OrderProducts er en forbindelsestabel,
    // fordi én ordre kan have flere produkter.
    public void AddProductToOrder(
        int orderId,
        int productId,
        int quantity,
        decimal priceAtPurchase)
    {
        // Antallet skal altid være mindst 1.
        if (quantity <= 0)
        {
            throw new ArgumentException(
                "Quantity must be greater than zero.");
        }

        string sql = """
            INSERT INTO paperpetals.OrderProducts
            (
                OrderId,
                ProductId,
                Quantity,
                PriceAtPurchase
            )
            VALUES
            (
                @OrderId,
                @ProductId,
                @Quantity,
                @PriceAtPurchase
            );
            """;

        using SqlConnection connection =
            new SqlConnection(_connectionString);

        using SqlCommand command =
            new SqlCommand(sql, connection);

        command.Parameters.AddWithValue(
            "@OrderId",
            orderId);

        command.Parameters.AddWithValue(
            "@ProductId",
            productId);

        command.Parameters.AddWithValue(
            "@Quantity",
            quantity);

        // PriceAtPurchase gemmer produktets pris,
        // som den var på købstidspunktet.
        //
        // Hvis produktets pris ændres senere,
        // skal gamle ordrer stadig vise den oprindelige pris.
        command.Parameters.AddWithValue(
            "@PriceAtPurchase",
            priceAtPurchase);

        connection.Open();

        command.ExecuteNonQuery();
    }


    // Opdaterer status på en ordre.
    public void UpdateStatus(
        int orderId,
        OrderStatus status)
    {
        string sql = """
            UPDATE paperpetals.Orders
            SET Status = @Status
            WHERE Id = @OrderId;
            """;

        using SqlConnection connection =
            new SqlConnection(_connectionString);

        using SqlCommand command =
            new SqlCommand(sql, connection);

        command.Parameters.AddWithValue(
            "@OrderId",
            orderId);

        command.Parameters.AddWithValue(
            "@Status",
            status.ToString());

        connection.Open();

        command.ExecuteNonQuery();
    }


    // Denne hjælpemetode laver én række fra Orders-tabellen
    // om til et Order-objekt.
    private static Order CreateOrder(
        SqlDataReader reader)
    {
        return new Order
        {
            Id = reader.GetInt32(0),

            CustomerId = reader.GetInt32(1),

            OrderDate = reader.GetDateTime(2),

            TotalPrice = reader.GetDecimal(3),

            // Status ligger som tekst i databasen.
            // Enum.Parse omdanner teksten til vores OrderStatus-enum.
            Status = Enum.Parse<OrderStatus>(
                reader.GetString(4))
        };
    }
}