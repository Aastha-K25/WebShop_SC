
using Obligatoris.opgave.Domain.Models; // Giver adgang til vores modeller

namespace Obligatoris.opgave.Domain.Interfaces;

// Interface som bestemmer hvilke metoder PasswordResetTokenRepository skal have
// Interfacet beskriver hvad vi skal kunne gøre med password reset tokens
// Selve koden til metoderne ligger i PasswordResetTokenRepository
public interface IPasswordResetTokenRepository
{
    // Finder et bestemt token i databasen ud fra dets hash
    // Vi søger på hashen fordi det oprindelige token ikke gemmes i databasen
    // Spørgsmålstegnet betyder at resultatet må være null
    PasswordResetToken? GetByTokenHash(string tokenHash);

    // Gemmer et nyt password reset token i databasen
    // Modtager et PasswordResetToken objekt
    void Add(PasswordResetToken token);

    // Opdaterer et eksisterende token i databasen
    // Bruges blandt andet når tokenet skal markeres som brugt
    void Update(PasswordResetToken token);

    // Sletter tokens som er udløbet eller allerede brugt
    // Hjælper med at rydde op i databasen
    void DeleteExpiredTokens();
}
