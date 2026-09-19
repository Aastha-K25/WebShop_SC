using Obligatoris.opgave.Domain.Interfaces;
using Obligatoris.opgave.Domain.Models;

namespace Obligatorisk_opgave.Infrastructure.Repositories;

// UserRepository får ansvaret for kommunikationen
// mellem brugerdata og databasen.
public class UserRepository : IUserRepository
{
    public List<User> GetAll()
    {
        //To do: henter alle brugeren fra database - fikser når vi var en db
        throw new NotImplementedException();
    }

    public User? GetById(int id)
    {
        //TO dog finder brugeren ud fra deres id - fikser når vi var en db
        throw new NotImplementedException();
    }

    public User? GetByEmail(string email)
    {
        //To do finder brugeren via deres email - fikser når vi var en db
        throw new NotImplementedException();
    }

    public bool EmailExists(string email)
    {
        //TO do kontrolllere om email faktisk findes i db -fikser når vi var en db 
        throw new NotImplementedException();
    }

    public void Add(User user)
    {
        //To do gemmer en ny burger i db - fikser når vi var en db
        throw new NotImplementedException();
    }

    public void Update(User user)
    {
        //To do opdatere brugerens oplysninger i db - fikser når vi var en db
        throw new NotImplementedException();
    }

    public void Deactivate(int id)
    {
        //To do sætter bruegresn IsAktive til false i database - fikser når vi var en db
        throw new NotImplementedException();
    }
}