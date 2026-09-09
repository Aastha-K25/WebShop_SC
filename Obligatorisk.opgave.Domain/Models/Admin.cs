namespace Obligatoris.opgave.Domain.Models;

public class Admin : User
{
    public string EmployeeNumber { get; set; }
    public bool MfaEnabled { get; set; }
    
}