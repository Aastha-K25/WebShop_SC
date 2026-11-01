namespace Obligatoris.opgave.Domain.Models;

public class Admin : User
{
    public required string EmployeeNumber { get; set; }
    
    //Viser om adminsitartoren skal brueg MFA. Selve MFA-flowet er ikke implementerte men er sat klar til hvis det skal køres
    public bool MfaEnabled { get; set; }
    
}