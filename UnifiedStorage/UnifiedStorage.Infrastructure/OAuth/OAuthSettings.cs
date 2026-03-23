namespace UnifiedStorage.Infrastructure.OAuth;

public class OAuthSettings
{
    public const string SectionName = "OAuth";

    public OAuthProviderSettings GoogleDrive { get; set; } = new();
    public OAuthProviderSettings OneDrive { get; set; } = new();
    public OAuthProviderSettings Dropbox { get; set; } = new();
}
