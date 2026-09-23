namespace Obligatoris.opgave.Domain.Models;

public abstract class User
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    //Vi gemmer kun et hash af adgangskoden
    //må aldrig gemmes i klartekst
    public required string PasswordHash { get; set; }
    public bool IsActive { get; set; } = true;
}