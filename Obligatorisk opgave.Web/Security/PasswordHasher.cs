using Isopoh.Cryptography.Argon2;

namespace Obligatorisk_opgave.Security;

public class PasswordHasher
{
    //Hasher passwordet med Argon2
    //Argon2 opretter automatisk et unikt salt 
    public string HashPassword(string password)
    {
        string passwordHash = Argon2.Hash(password);
        
        return passwordHash;
    }
    
    //Kontrollerer om det indtastede password, passer til den hash, som er gemt i db. 
    public bool VerifyPassword(string password, string passwordHash)
    {
        bool passwordIsCorrect = Argon2.Verify(passwordHash, password);

        return passwordIsCorrect;
    }
}