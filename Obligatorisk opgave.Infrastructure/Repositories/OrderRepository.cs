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

        command.Parameters.AddWithValue(
            "@Id",
            id);

        connection.Open();

        using SqlDataReader reader =
            command.ExecuteReader();


        if (reader.Read())
        {
            return CreateOrder(reader);
        }


        return null;
    }


    // Henter alle ordrer, som tilhører en bestemt kunde.
    public List<Order> GetByCustomerId(int customerId)
    {
        List<Order> orders =
            new List<Order>();


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

        using SqlDataReader reader =
            command.ExecuteReader();


        while (reader.Read())
        {
            Order order =
                CreateOrder(reader);

            orders.Add(order);
        }


        return orders;
    }


    // Henter alle ordrer.
    // Bruges af Admin til at se og behandle ordrer.
    public List<Order> GetAll()
    {
        List<Order> orders =
            new List<Order>();


        string sql = """
            SELECT
                Id,
                CustomerId,
                OrderDate,
                TotalPrice,
                Status
            FROM paperpetals.Orders
            ORDER BY OrderDate DESC;
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
            Order order =
                CreateOrder(reader);

            orders.Add(order);
        }


        return orders;
    }


    // Henter produkterne, der hører til en bestemt ordre.
    public List<OrderProduct> GetOrderProducts(int orderId)
    {
        List<OrderProduct> orderProducts =
            new List<OrderProduct>();


        string sql = """
            SELECT
                OrderId,
                ProductId,
                Quantity,
                PriceAtPurchase
            FROM paperpetals.OrderProducts
            WHERE OrderId = @OrderId;
            """;


        using SqlConnection connection =
            new SqlConnection(_connectionString);

        using SqlCommand command =
            new SqlCommand(sql, connection);


        command.Parameters.AddWithValue(
            "@OrderId",
            orderId);


        connection.Open();

        using SqlDataReader reader =
            command.ExecuteReader();


        while (reader.Read())
        {
            OrderProduct orderProduct =
                new OrderProduct
                {
                    OrderId =
                        reader.GetInt32(0),

                    ProductId =
                        reader.GetInt32(1),

                    Quantity =
                        reader.GetInt32(2),

                    PriceAtPurchase =
                        reader.GetDecimal(3)
                };


            orderProducts.Add(
                orderProduct);
        }


        return orderProducts;
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
        command.Parameters.AddWithValue(
            "@TotalPrice",
            order.TotalPrice);


        command.Parameters.AddWithValue(
            "@Status",
            order.Status.ToString());


        connection.Open();


        int newOrderId =
            Convert.ToInt32(
                command.ExecuteScalar());


        order.Id =
            newOrderId;


        return newOrderId;
    }


    // Gemmer forbindelsen mellem en ordre og et produkt.
    public void AddProductToOrder(
        int orderId,
        int productId,
        int quantity,
        decimal priceAtPurchase)
    {
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


        // Gemmer prisen fra købstidspunktet.
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


    // Laver én række fra Orders-tabellen
    // om til et Order-objekt.
    private static Order CreateOrder(
        SqlDataReader reader)
    {
        return new Order
        {
            Id =
                reader.GetInt32(0),

            CustomerId =
                reader.GetInt32(1),

            OrderDate =
                reader.GetDateTime(2),

            TotalPrice =
                reader.GetDecimal(3),

            Status =
                Enum.Parse<OrderStatus>(
                    reader.GetString(4))
        };
    }
}