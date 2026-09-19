namespace Obligatoris.opgave.Domain.Models;

public class Customer : User
{
    public required string Address { get; set; }
}