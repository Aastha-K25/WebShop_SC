namespace Obligatoris.opgave.Domain.Models;

public class PasswordResetToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public required string TokenHash { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }

    public bool IsValid()
    {
        bool hasNotBeenUsed = UsedAt == null;
        bool hasNotExpired = ExpiresAt > DateTime.UtcNow;

        return hasNotBeenUsed && hasNotExpired;
    }
}