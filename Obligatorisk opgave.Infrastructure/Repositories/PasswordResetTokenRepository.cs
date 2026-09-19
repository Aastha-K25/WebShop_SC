using Obligatoris.opgave.Domain.Interfaces;
using Obligatoris.opgave.Domain.Models;

namespace Obligatorisk_opgave.Infrastructure.Repositories;

// Repositoryet får ansvaret for at gemme og hente
// password reset-tokens fra databasen.
public class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    public PasswordResetToken? GetByTokenHash(string tokenHash)
    {
        //To do: find tokenet i db ude fra tokenst hash 
        throw new NotImplementedException();
    }

    public void Add(PasswordResetToken token)
    {
        //To do: gem et nyt token i db
        throw new NotImplementedException();
    }

    public void Update(PasswordResetToken token)
    {
        //to do opdatere tokenet i db
        throw new NotImplementedException();
    }

    public void DeleteExpiredTokens()
    {
        //to do sletter token hvor expries er overskrevt - 
        throw new NotImplementedException();
    }
}