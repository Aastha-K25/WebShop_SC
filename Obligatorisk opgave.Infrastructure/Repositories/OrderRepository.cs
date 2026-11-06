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


    // Gennemfører checkout i én samlet transaction
    // Opretter ordre, opdaterer lager og tømmer kurven
    public int PlaceOrder(
        int customerId,
        decimal shippingPrice)
    {
        // Kontrollerer at kunde og leveringspris er gyldige
        if (customerId <= 0 || shippingPrice < 0)
        {
            throw new ArgumentException(
                "Invalid checkout information.");
        }


        using SqlConnection connection =
            new SqlConnection(_connectionString);

        connection.Open();


        // Alle databaseændringer skal gennemføres samlet
        // Serializable beskytter også ved samtidige køb
        using SqlTransaction transaction =
            connection.BeginTransaction(
                System.Data.IsolationLevel.Serializable);

        try
        {
            // Finder kundens egen kurv
            string cartSql = """
                SELECT Id
                FROM paperpetals.ShoppingCarts
                WHERE CustomerId = @CustomerId;
                """;

            using SqlCommand cartCommand =
                new SqlCommand(
                    cartSql,
                    connection,
                    transaction);

            cartCommand.Parameters.AddWithValue(
                "@CustomerId",
                customerId);

            object? cartResult =
                cartCommand.ExecuteScalar();

            if (cartResult == null)
            {
                throw new InvalidOperationException(
                    "Shopping cart was not found.");
            }

            int cartId =
                Convert.ToInt32(cartResult);


            // Henter antal, pris og lager direkte fra databasen
            string productsSql = """
                SELECT
                    p.Id,
                    scp.Quantity,
                    p.Price,
                    p.StockQuantity
                FROM paperpetals.ShoppingCartProducts scp
                INNER JOIN paperpetals.Products p
                    ON scp.ProductId = p.Id
                WHERE scp.ShoppingCartId = @ShoppingCartId
                ORDER BY p.Id;
                """;

            using SqlCommand productsCommand =
                new SqlCommand(
                    productsSql,
                    connection,
                    transaction);

            productsCommand.Parameters.AddWithValue(
                "@ShoppingCartId",
                cartId);


            // Gemmer oplysningerne midlertidigt
            List<(int ProductId, int Quantity, decimal Price)>
                products =
                    new List<(int, int, decimal)>();

            decimal subtotal = 0;


            using (SqlDataReader reader =
                   productsCommand.ExecuteReader())
            {
                while (reader.Read())
                {
                    int productId =
                        reader.GetInt32(0);

                    int quantity =
                        reader.GetInt32(1);

                    decimal price =
                        reader.GetDecimal(2);

                    int stockQuantity =
                        reader.GetInt32(3);


                    // Antal og lager kontrolleres igen
                    if (quantity <= 0 ||
                        quantity > stockQuantity ||
                        price < 0)
                    {
                        throw new InvalidOperationException(
                            "A product is unavailable or has invalid information.");
                    }


                    subtotal =
                        subtotal + (price * quantity);

                    products.Add((
                        productId,
                        quantity,
                        price));
                }
            }


            // En tom kurv må ikke oprette en ordre
            if (products.Count == 0)
            {
                throw new InvalidOperationException(
                    "Shopping cart is empty.");
            }


            decimal totalPrice =
                subtotal + shippingPrice;


            // Opretter ordren
            string orderSql = """
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

            using SqlCommand orderCommand =
                new SqlCommand(
                    orderSql,
                    connection,
                    transaction);

            orderCommand.Parameters.AddWithValue(
                "@CustomerId",
                customerId);

            orderCommand.Parameters.AddWithValue(
                "@OrderDate",
                DateTime.UtcNow);

            orderCommand.Parameters.Add(
                "@TotalPrice",
                System.Data.SqlDbType.Decimal
                ).Value = totalPrice;

            orderCommand.Parameters.AddWithValue(
                "@Status",
                OrderStatus.Pending.ToString());


            int orderId =
                Convert.ToInt32(
                    orderCommand.ExecuteScalar());


            // Gemmer produkterne på ordren
            foreach (var product in products)
            {
                string orderProductSql = """
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

                using SqlCommand orderProductCommand =
                    new SqlCommand(
                        orderProductSql,
                        connection,
                        transaction);

                orderProductCommand.Parameters.AddWithValue(
                    "@OrderId",
                    orderId);

                orderProductCommand.Parameters.AddWithValue(
                    "@ProductId",
                    product.ProductId);

                orderProductCommand.Parameters.AddWithValue(
                    "@Quantity",
                    product.Quantity);

                orderProductCommand.Parameters.AddWithValue(
                    "@PriceAtPurchase",
                    product.Price);

                orderProductCommand.ExecuteNonQuery();


                // Trækker antal fra lageret
                // Stopper hvis der ikke længere er nok på lager
                string stockSql = """
                    UPDATE paperpetals.Products
                    SET StockQuantity = StockQuantity - @Quantity
                    WHERE Id = @ProductId
                      AND StockQuantity >= @Quantity;
                    """;

                using SqlCommand stockCommand =
                    new SqlCommand(
                        stockSql,
                        connection,
                        transaction);

                stockCommand.Parameters.AddWithValue(
                    "@ProductId",
                    product.ProductId);

                stockCommand.Parameters.AddWithValue(
                    "@Quantity",
                    product.Quantity);

                int updatedRows =
                    stockCommand.ExecuteNonQuery();


                if (updatedRows != 1)
                {
                    throw new InvalidOperationException(
                        "Not enough products in stock.");
                }
            }


            // Tømmer først kurven når alle produkter er behandlet
            string clearCartSql = """
                DELETE FROM paperpetals.ShoppingCartProducts
                WHERE ShoppingCartId = @ShoppingCartId;
                """;

            using SqlCommand clearCartCommand =
                new SqlCommand(
                    clearCartSql,
                    connection,
                    transaction);

            clearCartCommand.Parameters.AddWithValue(
                "@ShoppingCartId",
                cartId);

            int deletedRows =
                clearCartCommand.ExecuteNonQuery();

            if (deletedRows != products.Count)
            {
                throw new InvalidOperationException(
                    "Shopping cart changed during checkout.");
            }


            // Alle ændringer gemmes samlet
            transaction.Commit();

            return orderId;
        }
        catch
        {
            // Hvis noget fejler bliver alle ændringer fortrudt
            transaction.Rollback();

            throw;
        }
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