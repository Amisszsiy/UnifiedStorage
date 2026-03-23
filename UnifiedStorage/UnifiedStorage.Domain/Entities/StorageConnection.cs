using UnifiedStorage.Domain.Enums;

namespace UnifiedStorage.Domain.Entities;

public class StorageConnection
{
    private StorageConnection() { } // EF Core

    public Guid Id { get; private set; }
    public string UserId { get; private set; } = null!;
    public StorageProvider Provider { get; private set; }
    public string EncryptedAccessToken { get; private set; } = null!;
    public string EncryptedRefreshToken { get; private set; } = null!;
    public DateTime AccessTokenExpiresAt { get; private set; }
    public ConnectionStatus Status { get; private set; }
    public DateTime LastUsedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static StorageConnection Create(
        string userId,
        StorageProvider provider,
        string encryptedAccessToken,
        string encryptedRefreshToken,
        DateTime accessTokenExpiresAt)
    {
        return new StorageConnection
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = provider,
            EncryptedAccessToken = encryptedAccessToken,
            EncryptedRefreshToken = encryptedRefreshToken,
            AccessTokenExpiresAt = accessTokenExpiresAt,
            Status = ConnectionStatus.Active,
            LastUsedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateTokens(string encryptedAccessToken, string encryptedRefreshToken, DateTime accessTokenExpiresAt)
    {
        EncryptedAccessToken = encryptedAccessToken;
        EncryptedRefreshToken = encryptedRefreshToken;
        AccessTokenExpiresAt = accessTokenExpiresAt;
        Status = ConnectionStatus.Active;
        LastUsedAt = DateTime.UtcNow;
    }

    public void MarkAsExpired() => Status = ConnectionStatus.Expired;
    public void MarkAsRevoked() => Status = ConnectionStatus.Revoked;
    public void Touch() => LastUsedAt = DateTime.UtcNow;

    public bool IsAccessTokenExpired() => DateTime.UtcNow >= AccessTokenExpiresAt;
    public bool IsAccessTokenExpiringSoon(int minutesThreshold = 5)
        => DateTime.UtcNow >= AccessTokenExpiresAt.AddMinutes(-minutesThreshold);
}
