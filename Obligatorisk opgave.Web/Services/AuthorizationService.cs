
using Obligatoris.opgave.Domain.Models; // Giver adgang til vores modeller

namespace Obligatorisk_opgave.Services;

// Service som kontrollerer hvad de forskellige brugere må gøre
public class AuthorizationService
{
    // Kontrollerer om brugeren må administrere produkter
    public bool CanManageProducts(User? user)
    {
        // Returnerer true hvis brugeren er Admin og ellers false
        return user is Admin;
    }

    // Kontrollerer om brugeren må administrere ordrer
    public bool CanManageOrders(User? user)
    {
        // Kun Admin får adgang til at administrere ordrer
        return user is Admin;
    }

    // Kontrollerer om brugeren må administrere andre brugere
    public bool CanManageUsers(User? user)
    {
        // Kun Admin får adgang til at administrere brugere
        return user is Admin;
    }

    // Kontrollerer om brugeren må gennemføre checkout
    public bool CanCheckout(User? user)
    {
        // Returnerer true hvis brugeren er Customer
        // Admin og gæster får false
        return user is Customer;
    }

    // Kontrollerer om brugeren har adgang til en bestemt ordre
    public bool CanViewOrder(User? user, Order order)
    {
        // Tjekker om brugeren er en Admin
        if (user is Admin)
        {
            // Admin må se alle ordrer
            return true;
        }

        // Tjekker om brugeren er en Customer
        if (user is Customer customer)
        {
            // Kontrollerer om ordren tilhører kunden
            return OwnsOrder(customer, order);
        }

        // Afviser adgang hvis brugeren hverken er Admin eller Customer
        return false;
    }

    // Kontrollerer om en bestemt ordre tilhører kunden
    public bool OwnsOrder(Customer customer, Order order)
    {
        // Sammenligner ordreens kunde id med kundens eget id
        // Returnerer true hvis de er ens
        return order.CustomerId == customer.Id;
    }

    // Kontrollerer om en bestemt indkøbskurv tilhører kunden
    public bool OwnsCart(Customer customer, ShoppingCart shoppingCart)
    {
        // Sammenligner kurvens kunde id med kundens eget id
        // Returnerer true hvis de er ens
        return shoppingCart.CustomerId == customer.Id;
    }

    // Kontrollerer om kunden forsøger at åbne sin egen konto
    public bool OwnsAccount(Customer customer, int userId)
    {
        // Sammenligner kundens id med det ønskede bruger id
        // Returnerer true hvis de er ens
        return customer.Id == userId;
    }

    // Kontrollerer om brugeren må bruge password reset til kunder
    public bool CanUseCustomerPasswordReset(User? user)
    {
        // Returnerer false hvis brugeren er Admin
        // Customer og gæster får true
        return user is not Admin;
    }
}
