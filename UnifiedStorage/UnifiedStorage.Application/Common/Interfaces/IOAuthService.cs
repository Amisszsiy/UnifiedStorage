using UnifiedStorage.Application.Common.Models;
using UnifiedStorage.Domain.Enums;

namespace UnifiedStorage.Application.Common.Interfaces;

public interface IOAuthService
{
    string GetAuthorizationUrl(StorageProvider provider, string state, string redirectUri);

    Task<OAuthTokenResult> ExchangeCodeForTokensAsync(
        StorageProvider provider,
        string code,
        string redirectUri,
        CancellationToken cancellationToken = default);

    Task<OAuthTokenResult> RefreshTokenAsync(
        StorageProvider provider,
        string refreshToken,
        CancellationToken cancellationToken = default);
}
