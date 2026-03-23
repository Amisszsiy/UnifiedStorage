using System.Net.Http.Json;
using System.Web;
using Microsoft.Extensions.Options;
using UnifiedStorage.Application.Common.Interfaces;
using UnifiedStorage.Application.Common.Models;
using UnifiedStorage.Domain.Enums;

namespace UnifiedStorage.Infrastructure.OAuth;

public class OAuthService : IOAuthService
{
    private readonly HttpClient _httpClient;
    private readonly OAuthSettings _settings;

    private static readonly Dictionary<StorageProvider, (string AuthUrl, string TokenUrl, string Scope)> ProviderConfig = new()
    {
        [StorageProvider.GoogleDrive] = (
            "https://accounts.google.com/o/oauth2/v2/auth",
            "https://oauth2.googleapis.com/token",
            "https://www.googleapis.com/auth/drive"),
        [StorageProvider.OneDrive] = (
            "https://login.microsoftonline.com/common/oauth2/v2.0/authorize",
            "https://login.microsoftonline.com/common/oauth2/v2.0/token",
            "Files.ReadWrite.All offline_access"),
        [StorageProvider.Dropbox] = (
            "https://www.dropbox.com/oauth2/authorize",
            "https://api.dropbox.com/oauth2/token",
            "files.content.write files.content.read")
    };

    public OAuthService(HttpClient httpClient, IOptions<OAuthSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
    }

    public string GetAuthorizationUrl(StorageProvider provider, string state, string redirectUri)
    {
        var (authUrl, _, scope) = ProviderConfig[provider];
        var clientId = GetClientId(provider);

        var query = HttpUtility.ParseQueryString(string.Empty);
        query["client_id"] = clientId;
        query["redirect_uri"] = redirectUri;
        query["response_type"] = "code";
        query["scope"] = scope;
        query["state"] = state;
        query["access_type"] = "offline"; // Google-specific, ignored by others

        return $"{authUrl}?{query}";
    }

    public async Task<OAuthTokenResult> ExchangeCodeForTokensAsync(
        StorageProvider provider,
        string code,
        string redirectUri,
        CancellationToken cancellationToken = default)
    {
        var (_, tokenUrl, _) = ProviderConfig[provider];

        var formData = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["client_id"] = GetClientId(provider),
            ["client_secret"] = GetClientSecret(provider)
        };

        return await PostTokenRequestAsync(tokenUrl, formData, cancellationToken);
    }

    public async Task<OAuthTokenResult> RefreshTokenAsync(
        StorageProvider provider,
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var (_, tokenUrl, _) = ProviderConfig[provider];

        var formData = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["client_id"] = GetClientId(provider),
            ["client_secret"] = GetClientSecret(provider)
        };

        var result = await PostTokenRequestAsync(tokenUrl, formData, cancellationToken);

        // Dropbox rotates refresh tokens; if new one not returned, keep old one
        if (string.IsNullOrEmpty(result.RefreshToken))
            result.RefreshToken = refreshToken;

        return result;
    }

    private async Task<OAuthTokenResult> PostTokenRequestAsync(
        string tokenUrl,
        Dictionary<string, string> formData,
        CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsync(
            tokenUrl,
            new FormUrlEncodedContent(formData),
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(
            cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Empty token response from provider.");

        return new OAuthTokenResult
        {
            AccessToken = tokenResponse.AccessToken,
            RefreshToken = tokenResponse.RefreshToken ?? string.Empty,
            ExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn)
        };
    }

    private string GetClientId(StorageProvider provider) => provider switch
    {
        StorageProvider.GoogleDrive => _settings.GoogleDrive.ClientId,
        StorageProvider.OneDrive => _settings.OneDrive.ClientId,
        StorageProvider.Dropbox => _settings.Dropbox.ClientId,
        _ => throw new ArgumentOutOfRangeException(nameof(provider))
    };

    private string GetClientSecret(StorageProvider provider) => provider switch
    {
        StorageProvider.GoogleDrive => _settings.GoogleDrive.ClientSecret,
        StorageProvider.OneDrive => _settings.OneDrive.ClientSecret,
        StorageProvider.Dropbox => _settings.Dropbox.ClientSecret,
        _ => throw new ArgumentOutOfRangeException(nameof(provider))
    };

    private class TokenResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = null!;

        [System.Text.Json.Serialization.JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
