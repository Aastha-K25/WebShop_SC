
namespace Obligatorisk_opgave.Security;

// Klasse som kontrollerer om et password følger vores sikkerhedsregler
public static class PasswordValidator
{
    // Static betyder at vi kan bruge klassen uden at oprette et objekt
    // HashSet bruges til at gemme en samling af forbudte passwords
    // readonly betyder at vi ikke kan udskifte selve listen med en ny
    // OrdinalIgnoreCase gør at store og små bogstaver behandles ens
    private static readonly HashSet<string> ForbiddenPasswords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        // Almindelige og usikre passwords som ikke må bruges
        "password",
        "password123",
        "123456",
        "12345678",
        "123456789",
        "qwerty",
        "paperandpetals",
        "paper&petals"
    };

    // Metode som kontrollerer password ud fra vores password policy
    // Returnerer en fejlbesked hvis passwordet er ugyldigt
    // Returnerer null hvis alle regler er opfyldt
    public static string? Validate(string password, string name, string email)
    {
        // Kontrollerer om passwordet er tomt eller kun indeholder mellemrum
        if (string.IsNullOrWhiteSpace(password))
        {
            // Stopper hvis brugeren ikke har skrevet et password
            return "Please enter a password.";
        }

        // Kontrollerer om passwordet er kortere end 15 tegn
        if (password.Length < 15)
        {
            // Afviser passwordet hvis det er for kort
            return "The password must contain at least 15 characters.";
        }

        // Kontrollerer om passwordet er længere end 64 tegn
        if (password.Length > 64)
        {
            // Afviser passwordet hvis det er for langt
            return "The password cannot contain more than 64 characters.";
        }

        // Kontrollerer om passwordet findes i listen over forbudte passwords
        if (ForbiddenPasswords.Contains(password))
        {
            // Afviser passwordet hvis det er et almindeligt eller usikkert password
            return "This password is too common. Please choose another password.";
        }

        // Kontrollerer om passwordet er det samme som brugerens navn
        // OrdinalIgnoreCase betyder at store og små bogstaver er ligegyldige
        if (password.Equals(name, StringComparison.OrdinalIgnoreCase))
        {
            // Afviser passwordet hvis det er det samme som navnet
            return "The password cannot be the same as your name.";
        }

        // Kontrollerer om passwordet er det samme som brugerens email
        if (password.Equals(email, StringComparison.OrdinalIgnoreCase))
        {
            // Afviser passwordet hvis det er det samme som email
            return "The password cannot be the same as your email.";
        }

        // Split deler email ved @ og [0] tager den første del
        // Eksempel sara@gmail.com bliver til sara
        string emailName = email.Split('@')[0];

        // Kontrollerer om passwordet er det samme som navnet fra email
        if (password.Equals(emailName, StringComparison.OrdinalIgnoreCase))
        {
            // Afviser passwordet hvis det matcher emailnavnet
            return "The password is too similar to your email.";
        }

        // Returnerer null hvis passwordet har bestået alle kontroller
        return null;
    }
}
