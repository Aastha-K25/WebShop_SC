using Obligatoris.opgave.Domain.Models;

namespace Obligatoris.opgave.Domain.Interfaces;

// Interfacet beskriver de funktioner,
// som UserRepository implementerer.
public interface IUserRepository
{
    // Henter alle brugere
    List<User> GetAll();

    // Finder en bruger ud fra brugerens id
    User? GetById(int id);

    // Finder en bruger ud fra e-mailadressen
    User? GetByEmail(string email);

    // Kontrollerer om e-mailadressen allerede findes
    bool EmailExists(string email);

    // Gemmer en ny bruger
    void Add(User user);

    // Opdaterer en eksisterende bruger
    void Update(User user);

    // Deaktiverer en bruger uden at slette data
    void Deactivate(int id);
}