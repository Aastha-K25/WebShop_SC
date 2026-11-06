
using Isopoh.Cryptography.Argon2; // Giver adgang til Argon2 som bruges til at hashe passwords

namespace Obligatorisk_opgave.Security;

// Klasse som håndterer hashing og kontrol af passwords
public class PasswordHasher
{
    // Metode som laver et password om til en hash
    public string HashPassword(string password)
    {
        // Argon2 hasher passwordet og laver automatisk et salt
        // Salt gør at ens passwords ikke nødvendigvis får samme hash
        string passwordHash = Argon2.Hash(password);

        // Returnerer den færdige hash som skal gemmes i databasen
        return passwordHash;
    }

    // Metode som kontrollerer om et password passer til den gemte hash
    public bool VerifyPassword(string password, string passwordHash)
    {
        // Argon2 sammenligner passwordet med den gemte hash
        // Returnerer true hvis passwordet passer og ellers false
        bool passwordIsCorrect = Argon2.Verify(passwordHash, password);

        // Returnerer resultatet af kontrollen
        return passwordIsCorrect;
    }
}
