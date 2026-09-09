namespace Obligatoris.opgave.Domain.Models;

public class PasswordResetToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string TokenHash { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }

    public bool IsValid()
    {
        return UsedAt == null && ExpiresAt > DateTime.UtcNow;
    }
}