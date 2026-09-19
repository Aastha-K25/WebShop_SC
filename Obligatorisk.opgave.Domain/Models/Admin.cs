namespace Obligatoris.opgave.Domain.Models;

public class Admin : User
{
    public required string EmployeeNumber { get; set; }
    public bool MfaEnabled { get; set; }
    
}