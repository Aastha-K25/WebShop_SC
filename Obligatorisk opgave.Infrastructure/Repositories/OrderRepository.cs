
using Microsoft.Data.SqlClient; // Bruges til at arbejde med SQL Server
using Microsoft.Extensions.Configuration; // Bruges til at hente indstillinger
using Obligatoris.opgave.Domain.Interfaces; // Giver adgang til vores interfaces
using Obligatoris.opgave.Domain.Models; // Giver adgang til vores modeller

namespace Obligatorisk_opgave.Infrastructure.Repositories;

// Repository som håndterer ordrer i databasen
public class OrderRepository : IOrderRepository
{
    // Gemmer forbindelsesoplysningerne til databasen
    private readonly string _connectionString;

    // Constructor som får adgang til vores konfiguration
    public OrderRepository(IConfiguration configuration)
    {
        // Henter connection string fra konfigurationen
        // Stopper med en fejl hvis den ikke findes
        _connectionString = configuration.GetConnectionString("PaperPetalsDatabase")
            ?? throw new InvalidOperationException("Database connection string was not found.");
    }

    // Henter en bestemt ordre ud fra dens id
    public Order? GetById(int id)
    {
        // SQL som vælger oplysningerne fra Orders tabellen
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

        // Opretter forbindelsen til databasen
        using SqlConnection connection = new SqlConnection(_connectionString);

        // Gør vores SQL klar til at blive kørt
        using SqlCommand command = new SqlCommand(sql, connection);

        // Sender id som en parameter så vi undgår SQL injection
        command.Parameters.AddWithValue("@Id", id);

        // Åbner forbindelsen til databasen
        connection.Open();

        // Kører SQL og læser resultatet
        using SqlDataReader reader = command.ExecuteReader();

        // Tjekker om databasen fandt en ordre
        if (reader.Read())
        {
            // Laver resultatet om til et Order objekt
            return CreateOrder(reader);
        }

        // Returnerer null hvis ordren ikke findes
        return null;
    }

    // Henter alle ordrer som tilhører en bestemt kunde
    public List<Order> GetByCustomerId(int customerId)
    {
        // Opretter en tom liste til kundens ordrer
        List<Order> orders = new List<Order>();

        // Henter kundens ordrer med de nyeste først
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

        // Opretter forbindelsen til databasen
        using SqlConnection connection = new SqlConnection(_connectionString);

        // Gør SQL forespørgslen klar
        using SqlCommand command = new SqlCommand(sql, connection);

        // Sender kundens id sikkert som en parameter
        command.Parameters.AddWithValue("@CustomerId", customerId);

        // Åbner forbindelsen
        connection.Open();

        // Henter resultaterne fra databasen
        using SqlDataReader reader = command.ExecuteReader();

        // Går igennem alle ordrer som blev fundet
        while (reader.Read())
        {
            // Laver rækken fra databasen om til et Order objekt
            Order order = CreateOrder(reader);

            // Tilføjer ordren til listen
            orders.Add(order);
        }

        // Returnerer listen med kundens ordrer
        return orders;
    }

    // Henter alle ordrer fra databasen
    // Kan bruges af admin til at se og behandle ordrer
    public List<Order> GetAll()
    {
        // Opretter en tom liste til alle ordrer
        List<Order> orders = new List<Order>();

        // Henter alle ordrer med de nyeste først
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

        // Opretter forbindelsen til databasen
        using SqlConnection connection = new SqlConnection(_connectionString);

        // Gør SQL forespørgslen klar
        using SqlCommand command = new SqlCommand(sql, connection);

        // Åbner forbindelsen
        connection.Open();

        // Kører SQL og læser resultaterne
        using SqlDataReader reader = command.ExecuteReader();

        // Går igennem alle ordrer
        while (reader.Read())
        {
            // Laver resultatet om til et Order objekt
            Order order = CreateOrder(reader);

            // Tilføjer ordren til listen
            orders.Add(order);
        }

        // Returnerer alle ordrerne
        return orders;
    }

    // Henter produkterne som hører til en bestemt ordre
    public List<OrderProduct> GetOrderProducts(int orderId)
    {
        // Opretter en tom liste til ordreprodukterne
        List<OrderProduct> orderProducts = new List<OrderProduct>();

        // Henter produkter som hører til det valgte ordre id
        string sql = """
            SELECT
                OrderId,
                ProductId,
                Quantity,
                PriceAtPurchase
            FROM paperpetals.OrderProducts
            WHERE OrderId = @OrderId;
            """;

        // Opretter forbindelsen til databasen
        using SqlConnection connection = new SqlConnection(_connectionString);

        // Gør SQL forespørgslen klar
        using SqlCommand command = new SqlCommand(sql, connection);

        // Sender ordre id som en parameter
        command.Parameters.AddWithValue("@OrderId", orderId);

        // Åbner forbindelsen
        connection.Open();

        // Kører SQL og læser resultaterne
        using SqlDataReader reader = command.ExecuteReader();

        // Går igennem alle produkterne på ordren
        while (reader.Read())
        {
            // Opretter et nyt OrderProduct objekt
            OrderProduct orderProduct = new OrderProduct
            {
                // Henter ordre id fra første kolonne
                OrderId = reader.GetInt32(0),

                // Henter produkt id fra anden kolonne
                ProductId = reader.GetInt32(1),

                // Henter antal fra tredje kolonne
                Quantity = reader.GetInt32(2),

                // Henter prisen fra købstidspunktet
                PriceAtPurchase = reader.GetDecimal(3)
            };

            // Tilføjer produktet til listen
            orderProducts.Add(orderProduct);
        }

        // Returnerer listen med ordreprodukterne
        return orderProducts;
    }

    // Gemmer en ny ordre i databasen
    public int Add(Order order)
    {
        // SQL som indsætter en ny ordre i Orders tabellen
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

        // Opretter forbindelsen til databasen
        using SqlConnection connection = new SqlConnection(_connectionString);

        // Gør SQL forespørgslen klar
        using SqlCommand command = new SqlCommand(sql, connection);

        // Sender kundens id som en parameter
        command.Parameters.AddWithValue("@CustomerId", order.CustomerId);

        // Sender datoen for ordren
        command.Parameters.AddWithValue("@OrderDate", order.OrderDate);

        // Sender den samlede pris som skal være beregnet i backend
        command.Parameters.AddWithValue("@TotalPrice", order.TotalPrice);

        // Laver ordrestatus om til tekst før den gemmes
        command.Parameters.AddWithValue("@Status", order.Status.ToString());

        // Åbner forbindelsen
        connection.Open();

        // Opretter ordren og henter dens nye id
        int newOrderId = Convert.ToInt32(command.ExecuteScalar());

        // Gemmer det nye id i vores Order objekt
        order.Id = newOrderId;

        // Returnerer id på den nye ordre
        return newOrderId;
    }

    // Gemmer et produkt på en bestemt ordre
    public void AddProductToOrder(
        int orderId,
        int productId,
        int quantity,
        decimal priceAtPurchase)
    {
        // Kontrollerer at antal er større end nul
        if (quantity <= 0)
        {
            // Stopper hvis antal er ugyldigt
            throw new ArgumentException("Quantity must be greater than zero.");
        }

        // SQL som forbinder produktet med ordren
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

        // Opretter forbindelsen til databasen
        using SqlConnection connection = new SqlConnection(_connectionString);

        // Gør SQL forespørgslen klar
        using SqlCommand command = new SqlCommand(sql, connection);

        // Sender id på ordren
        command.Parameters.AddWithValue("@OrderId", orderId);

        // Sender id på produktet
        command.Parameters.AddWithValue("@ProductId", productId);

        // Sender hvor mange der er købt
        command.Parameters.AddWithValue("@Quantity", quantity);

        // Gemmer prisen som produktet havde da det blev købt
        command.Parameters.AddWithValue("@PriceAtPurchase", priceAtPurchase);

        // Åbner forbindelsen
        connection.Open();

        // Kører SQL og gemmer produktet på ordren
        command.ExecuteNonQuery();
    }

    // Gennemfører kundens køb
    // Opretter ordre, opdaterer lager og tømmer kurven
    public int PlaceOrder(
        int customerId,
        decimal shippingPrice)
    {
        // Kontrollerer at kunde id er gyldigt og fragt ikke er negativ
        if (customerId <= 0 || shippingPrice < 0)
        {
            // Stopper hvis oplysningerne er ugyldige
            throw new ArgumentException("Invalid checkout information.");
        }

        // Opretter forbindelsen til databasen
        using SqlConnection connection = new SqlConnection(_connectionString);

        // Åbner forbindelsen
        connection.Open();

        // Starter en transaction så ændringerne hænger sammen
        // Serializable hjælper med at beskytte mod samtidige ændringer
        using SqlTransaction transaction = connection.BeginTransaction(
            System.Data.IsolationLevel.Serializable);

        // Forsøger at gennemføre hele købet
        try
        {
            // SQL som finder kundens indkøbskurv
            string cartSql = """
                SELECT Id
                FROM paperpetals.ShoppingCarts
                WHERE CustomerId = @CustomerId;
                """;

            // Gør SQL klar og knytter den til vores transaction
            using SqlCommand cartCommand = new SqlCommand(
                cartSql, connection, transaction);

            // Sender kundens id som en parameter
            cartCommand.Parameters.AddWithValue("@CustomerId", customerId);

            // Henter id på kundens indkøbskurv
            object? cartResult = cartCommand.ExecuteScalar();

            // Tjekker om kunden har en indkøbskurv
            if (cartResult == null)
            {
                // Stopper hvis kurven ikke findes
                throw new InvalidOperationException("Shopping cart was not found.");
            }

            // Laver resultatet om til et heltal
            int cartId = Convert.ToInt32(cartResult);

            // Henter produkter, antal, pris og lager fra databasen
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

            // Gør SQL klar og bruger den samme transaction
            using SqlCommand productsCommand = new SqlCommand(
                productsSql, connection, transaction);

            // Sender indkøbskurvens id som en parameter
            productsCommand.Parameters.AddWithValue("@ShoppingCartId", cartId);

            // Opretter en liste med produkt id, antal og pris
            List<(int ProductId, int Quantity, decimal Price)> products =
                new List<(int, int, decimal)>();

            // Starter den samlede produktpris på nul
            decimal subtotal = 0;

            // Henter produkterne fra databasen
            using (SqlDataReader reader = productsCommand.ExecuteReader())
            {
                // Går igennem alle produkter i kurven
                while (reader.Read())
                {
                    // Henter produktets id
                    int productId = reader.GetInt32(0);

                    // Henter antal kunden vil købe
                    int quantity = reader.GetInt32(1);

                    // Henter produktets aktuelle pris
                    decimal price = reader.GetDecimal(2);

                    // Henter hvor mange der er på lager
                    int stockQuantity = reader.GetInt32(3);

                    // Kontrollerer antal, lager og pris
                    if (quantity <= 0 ||
                        quantity > stockQuantity ||
                        price < 0)
                    {
                        // Stopper hvis produktet ikke kan købes
                        throw new InvalidOperationException(
                            "A product is unavailable or has invalid information.");
                    }

                    // Lægger produktets pris gange antal til subtotalen
                    subtotal = subtotal + (price * quantity);

                    // Gemmer produktets oplysninger i listen
                    products.Add((productId, quantity, price));
                }
            }

            // Kontrollerer at kurven indeholder produkter
            if (products.Count == 0)
            {
                // Stopper hvis kurven er tom
                throw new InvalidOperationException("Shopping cart is empty.");
            }

            // Beregner den samlede pris inklusive fragt
            decimal totalPrice = subtotal + shippingPrice;

            // SQL som opretter den nye ordre
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

            // Gør SQL klar og bruger den samme transaction
            using SqlCommand orderCommand = new SqlCommand(
                orderSql, connection, transaction);

            // Sender kundens id
            orderCommand.Parameters.AddWithValue("@CustomerId", customerId);

            // Gemmer tidspunktet for ordren i UTC
            orderCommand.Parameters.AddWithValue("@OrderDate", DateTime.UtcNow);

            // Sender den samlede pris som decimal
            orderCommand.Parameters.Add(
                "@TotalPrice", System.Data.SqlDbType.Decimal).Value = totalPrice;

            // Giver den nye ordre status Pending
            orderCommand.Parameters.AddWithValue("@Status", OrderStatus.Pending.ToString());

            // Opretter ordren og henter dens nye id
            int orderId = Convert.ToInt32(orderCommand.ExecuteScalar());

            // Går igennem alle produkterne som kunden køber
            foreach (var product in products)
            {
                // SQL som gemmer produktet på ordren
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

                // Gør SQL klar og bruger den samme transaction
                using SqlCommand orderProductCommand = new SqlCommand(
                    orderProductSql, connection, transaction);

                // Sender id på den nye ordre
                orderProductCommand.Parameters.AddWithValue("@OrderId", orderId);

                // Sender produktets id
                orderProductCommand.Parameters.AddWithValue("@ProductId", product.ProductId);

                // Sender antal kunden har købt
                orderProductCommand.Parameters.AddWithValue("@Quantity", product.Quantity);

                // Gemmer produktets pris fra købstidspunktet
                orderProductCommand.Parameters.AddWithValue("@PriceAtPurchase", product.Price);

                // Gemmer produktet på ordren i databasen
                orderProductCommand.ExecuteNonQuery();

                // SQL som trækker det købte antal fra lageret
                // Opdaterer kun hvis der stadig er nok på lager
                string stockSql = """
                    UPDATE paperpetals.Products
                    SET StockQuantity = StockQuantity - @Quantity
                    WHERE Id = @ProductId
                      AND StockQuantity >= @Quantity;
                    """;

                // Gør SQL klar og bruger den samme transaction
                using SqlCommand stockCommand = new SqlCommand(
                    stockSql, connection, transaction);

                // Sender id på produktet som skal opdateres
                stockCommand.Parameters.AddWithValue("@ProductId", product.ProductId);

                // Sender hvor mange der skal trækkes fra lageret
                stockCommand.Parameters.AddWithValue("@Quantity", product.Quantity);

                // Opdaterer lageret og gemmer antal ændrede rækker
                int updatedRows = stockCommand.ExecuteNonQuery();

                // Kontrollerer at præcis én produktrække blev opdateret
                if (updatedRows != 1)
                {
                    // Stopper købet hvis lageret ikke kunne opdateres
                    throw new InvalidOperationException("Not enough products in stock.");
                }
            }

            // SQL som fjerner produkterne fra indkøbskurven
            string clearCartSql = """
                DELETE FROM paperpetals.ShoppingCartProducts
                WHERE ShoppingCartId = @ShoppingCartId;
                """;

            // Gør SQL klar og bruger den samme transaction
            using SqlCommand clearCartCommand = new SqlCommand(
                clearCartSql, connection, transaction);

            // Sender id på kurven som skal tømmes
            clearCartCommand.Parameters.AddWithValue("@ShoppingCartId", cartId);

            // Tømmer kurven og gemmer antal slettede rækker
            int deletedRows = clearCartCommand.ExecuteNonQuery();

            // Kontrollerer at det rigtige antal produkter blev fjernet
            if (deletedRows != products.Count)
            {
                // Stopper hvis kurven har ændret sig under købet
                throw new InvalidOperationException("Shopping cart changed during checkout.");
            }

            // Godkender og gemmer alle ændringer i databasen samlet
            transaction.Commit();

            // Returnerer id på den gennemførte ordre
            return orderId;
        }
        // Fanger fejl som opstår under købet
        catch
        {
            // Fortryder databaseændringerne hvis noget fejler
            transaction.Rollback();

            // Sender fejlen videre så den kan håndteres
            throw;
        }
    }

    // Opdaterer status på en bestemt ordre
    public void UpdateStatus(
        int orderId,
        OrderStatus status)
    {
        // SQL som ændrer status på den valgte ordre
        string sql = """
            UPDATE paperpetals.Orders
            SET Status = @Status
            WHERE Id = @OrderId;
            """;

        // Opretter forbindelsen til databasen
        using SqlConnection connection = new SqlConnection(_connectionString);

        // Gør SQL forespørgslen klar
        using SqlCommand command = new SqlCommand(sql, connection);

        // Sender id på ordren som skal opdateres
        command.Parameters.AddWithValue("@OrderId", orderId);

        // Laver den nye status om til tekst
        command.Parameters.AddWithValue("@Status", status.ToString());

        // Åbner forbindelsen til databasen
        connection.Open();

        // Kører SQL og opdaterer ordrens status
        command.ExecuteNonQuery();
    }

    // Hjælpemetode som laver data fra databasen om til en ordre
    private static Order CreateOrder(SqlDataReader reader)
    {
        // Opretter et nyt Order objekt med værdier fra databasen
        return new Order
        {
            // Henter ordrens id fra første kolonne
            Id = reader.GetInt32(0),

            // Henter kundens id fra anden kolonne
            CustomerId = reader.GetInt32(1),

            // Henter datoen fra tredje kolonne
            OrderDate = reader.GetDateTime(2),

            // Henter den samlede pris fra fjerde kolonne
            TotalPrice = reader.GetDecimal(3),

            // Henter status som tekst og laver den om til OrderStatus
            Status = Enum.Parse<OrderStatus>(reader.GetString(4))
        };
    }
}
