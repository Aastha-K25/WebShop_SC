using Obligatoris.opgave.Domain.Models;

namespace Obligatorisk_opgave.Services;

public class AuthorizationService
{
    // Kun Admin må administrere produkter
    public bool CanManageProducts(User? user)
    {
        return user is Admin;
    }


    // Kun Admin må administrere ordrer
    public bool CanManageOrders(User? user)
    {
        return user is Admin;
    }


    // Kun Admin må administrere brugere
    public bool CanManageUsers(User? user)
    {
        return user is Admin;
    }


    // Kun Customer må gennemføre checkout
    public bool CanCheckout(User? user)
    {
        return user is Customer;
    }


    // Customer må se egne ordrer.
    // Admin må se ordrer som del af administration.
    public bool CanViewOrder(
        User? user,
        Order order)
    {
        if (user is Admin)
        {
            return true;
        }


        if (user is Customer customer)
        {
            return OwnsOrder(
                customer,
                order);
        }


        return false;
    }


    // Kontrollerer om ordren tilhører kunden
    public bool OwnsOrder(
        Customer customer,
        Order order)
    {
        return order.CustomerId
            == customer.Id;
    }


    // Kontrollerer om kurven tilhører kunden
    public bool OwnsCart(
        Customer customer,
        ShoppingCart shoppingCart)
    {
        return shoppingCart.CustomerId
            == customer.Id;
    }


    // Kontrollerer om kunden tilgår sin egen konto
    public bool OwnsAccount(
        Customer customer,
        int userId)
    {
        return customer.Id
            == userId;
    }


    // Admin må ikke bruge customer password reset-flow
    public bool CanUseCustomerPasswordReset(
        User? user)
    {
        return user is not Admin;
    }
}