using UnifiedStorage.Application.Common.Interfaces;
using UnifiedStorage.Domain.Enums;
using UnifiedStorage.Domain.Models;

namespace UnifiedStorage.Infrastructure.Providers;

/// <summary>
/// OneDrive provider service via Microsoft Graph API.
/// Replace stub implementations with Microsoft.Graph NuGet package calls.
/// </summary>
public class OneDriveService : IStorageProviderService
{
    public StorageProvider Provider => StorageProvider.OneDrive;

    public Task<IReadOnlyList<CloudFile>> ListFilesAsync(
        string accessToken, string? folderId, CancellationToken cancellationToken = default)
    {
        // TODO: Use Microsoft.Graph
        // var authProvider = new BaseBearerTokenAuthenticationProvider(new TokenProvider(accessToken));
        // var graphClient = new GraphServiceClient(authProvider);
        // var items = folderId != null
        //     ? await graphClient.Me.Drive.Items[folderId].Children.GetAsync(cancellationToken: cancellationToken)
        //     : await graphClient.Me.Drive.Root.Children.GetAsync(cancellationToken: cancellationToken);
        throw new NotImplementedException("Install Microsoft.Graph NuGet package and implement this method.");
    }

    public Task<CloudFile> UploadFileAsync(
        string accessToken, string fileName, Stream content, string? folderId,
        CancellationToken cancellationToken = default)
    {
        // TODO: graphClient.Me.Drive.Items[folderId ?? "root"].ItemWithPath(fileName).Content.PutAsync(content)
        throw new NotImplementedException("Install Microsoft.Graph NuGet package and implement this method.");
    }

    public Task DeleteFileAsync(
        string accessToken, string fileId, CancellationToken cancellationToken = default)
    {
        // TODO: graphClient.Me.Drive.Items[fileId].DeleteAsync()
        throw new NotImplementedException("Install Microsoft.Graph NuGet package and implement this method.");
    }

    public Task<(Stream Content, string FileName, string ContentType)> DownloadFileAsync(
        string accessToken, string fileId, CancellationToken cancellationToken = default)
    {
        // TODO: graphClient.Me.Drive.Items[fileId].Content.GetAsync()
        throw new NotImplementedException("Install Microsoft.Graph NuGet package and implement this method.");
    }
}
