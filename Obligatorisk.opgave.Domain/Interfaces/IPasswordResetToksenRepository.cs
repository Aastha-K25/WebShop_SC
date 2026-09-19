using Obligatoris.opgave.Domain.Models;
namespace Obligatoris.opgave.Domain.Interfaces;

// Interfacet beskriver de databasefunktioner,
// der skal bruges ved nulstilling af adgangskoder.
public interface IPasswordResetTokenRepository
{
    // Finder et token ud fra tokenets hash
    PasswordResetToken? GetByTokenHash(string tokenHash);

    // Gemmer et nyt token
    void Add(PasswordResetToken token);

    // Opdaterer tokenet, eksempelvis når det er blevet brugt
    void Update(PasswordResetToken token);

    // Sletter udløbne tokens fra databasen
    void DeleteExpiredTokens();
}