using Domain.Commons;

namespace Domain.Entities;

public sealed class RefreshToken : BaseEntity
{
    public string Token { get; private set; } = null!;
    public Guid UserId { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public bool IsRevoked { get; private set; }

    public bool IsExpired => DateTime.UtcNow > ExpiresAtUtc;
    public bool IsActive => !IsRevoked && !IsExpired;

    private RefreshToken() { }

    public static RefreshToken Create(Guid userId, string token, DateTime expiresAtUtc)
        => new()
        {
            UserId = userId,
            Token = token,
            ExpiresAtUtc = expiresAtUtc,
            IsRevoked = false
        };

    public void Revoke()
    {
        IsRevoked = true;
        MarkAsUpdated();
    }

    public static class Errors
    {
        public static readonly Error Invalid = new("RefreshToken.Invalid", "El token de refresco es invalido o ha expirado.");
        public static readonly Error NotFound = new("RefreshToken.NotFound", "El token de refresco no existe.");
    }
}
