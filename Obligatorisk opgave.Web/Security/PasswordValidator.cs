namespace Obligatorisk_opgave.Security;

public static class PasswordValidator
{
    // Almindelige og svage passwords, som brugeren ikke må vælge.
    private static readonly HashSet<string> ForbiddenPasswords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "password",
            "password123",
            "123456",
            "12345678",
            "123456789",
            "qwerty",
            "paperandpetals",
            "paper&petals"
        };
    
    // Kontrollerer om passwordet følger vores password policy.
    // Metoden returnerer en fejlbesked, hvis passwordet ikke er gyldigt. Hvis passwordet er gyldigt, returnerer metoden null.
    public static string? Validate(string password, string name, string email)
    {
        // Passwordet må ikke være tomt.
        if (string.IsNullOrWhiteSpace(password))
        {
            return "Please enter a password.";
        }
        
        // Passwordet skal mindst indeholde 15 tegn.
        if (password.Length < 15)
        {
            return "The password must contain at least 15 characters.";
        }
        
        // Passwordet må højst indeholde 64 tegn.
        if (password.Length > 64)
        {
            return "The password cannot contain more than 64 characters.";
        }
        
        // Passwordet må ikke være et almindeligt og usikkert password.
        if (ForbiddenPasswords.Contains(password))
        {
            return "This password is too common. Please choose another password.";
        }
        
        // Passwordet må ikke være det samme som brugerens navn.
        if (password.Equals(name, StringComparison.OrdinalIgnoreCase))
        {
            return "The password cannot be the same as your name.";
        }
        
        // Passwordet må ikke være det samme som brugerens e-mail.
        if (password.Equals(email, StringComparison.OrdinalIgnoreCase))
        {
            return "The password cannot be the same as your email.";
        }
        
        // Henter teksten før @ i brugerens e-mail. Eksempel: sara@gmail.com bliver til sara.
        string emailName =  email.Split('@')[0];
        
        // Passwordet må ikke kun være navnet fra brugerens e-mail.
        if (password.Equals( emailName, StringComparison.OrdinalIgnoreCase))
        {
            return "The password is too similar to your email.";
        }
        
        // Null betyder, at passwordet er gyldigt.
        return null;
    }
}