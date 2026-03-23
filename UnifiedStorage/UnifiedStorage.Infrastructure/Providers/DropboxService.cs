using UnifiedStorage.Application.Common.Interfaces;
using UnifiedStorage.Domain.Enums;
using UnifiedStorage.Domain.Models;

namespace UnifiedStorage.Infrastructure.Providers;

/// <summary>
/// Dropbox provider service.
/// Replace stub implementations with Dropbox.Api NuGet package calls.
/// </summary>
public class DropboxService : IStorageProviderService
{
    public StorageProvider Provider => StorageProvider.Dropbox;

    public Task<IReadOnlyList<CloudFile>> ListFilesAsync(
        string accessToken, string? folderId, CancellationToken cancellationToken = default)
    {
        // TODO: Use Dropbox.Api
        // using var client = new DropboxClient(accessToken);
        // var result = await client.Files.ListFolderAsync(folderId ?? string.Empty);
        // map result.Entries to CloudFile list
        throw new NotImplementedException("Install Dropbox.Api NuGet package and implement this method.");
    }

    public Task<CloudFile> UploadFileAsync(
        string accessToken, string fileName, Stream content, string? folderId,
        CancellationToken cancellationToken = default)
    {
        // TODO: client.Files.UploadAsync(path, body: content)
        throw new NotImplementedException("Install Dropbox.Api NuGet package and implement this method.");
    }

    public Task DeleteFileAsync(
        string accessToken, string fileId, CancellationToken cancellationToken = default)
    {
        // TODO: client.Files.DeleteV2Async(fileId)
        throw new NotImplementedException("Install Dropbox.Api NuGet package and implement this method.");
    }

    public Task<(Stream Content, string FileName, string ContentType)> DownloadFileAsync(
        string accessToken, string fileId, CancellationToken cancellationToken = default)
    {
        // TODO: var response = await client.Files.DownloadAsync(fileId); response.GetContentAsStreamAsync()
        throw new NotImplementedException("Install Dropbox.Api NuGet package and implement this method.");
    }
}
