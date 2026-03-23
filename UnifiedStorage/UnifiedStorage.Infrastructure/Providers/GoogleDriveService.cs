using UnifiedStorage.Application.Common.Interfaces;
using UnifiedStorage.Domain.Enums;
using UnifiedStorage.Domain.Models;

namespace UnifiedStorage.Infrastructure.Providers;

/// <summary>
/// Google Drive provider service.
/// Replace stub implementations with Google.Apis.Drive.v3 NuGet package calls.
/// </summary>
public class GoogleDriveService : IStorageProviderService
{
    public StorageProvider Provider => StorageProvider.GoogleDrive;

    public Task<IReadOnlyList<CloudFile>> ListFilesAsync(
        string accessToken, string? folderId, CancellationToken cancellationToken = default)
    {
        // TODO: Use Google.Apis.Drive.v3
        // var credential = GoogleCredential.FromAccessToken(accessToken);
        // var service = new DriveService(new BaseClientService.Initializer { HttpClientInitializer = credential });
        // var request = service.Files.List(); request.Fields = "files(id,name,size,modifiedTime,mimeType,parents)";
        // if (folderId != null) request.Q = $"'{folderId}' in parents";
        // var result = await request.ExecuteAsync(cancellationToken);
        throw new NotImplementedException("Install Google.Apis.Drive.v3 NuGet package and implement this method.");
    }

    public Task<CloudFile> UploadFileAsync(
        string accessToken, string fileName, Stream content, string? folderId,
        CancellationToken cancellationToken = default)
    {
        // TODO: Use Google.Apis.Drive.v3 ResumableUpload for large files
        throw new NotImplementedException("Install Google.Apis.Drive.v3 NuGet package and implement this method.");
    }

    public Task DeleteFileAsync(
        string accessToken, string fileId, CancellationToken cancellationToken = default)
    {
        // TODO: service.Files.Delete(fileId).ExecuteAsync()
        throw new NotImplementedException("Install Google.Apis.Drive.v3 NuGet package and implement this method.");
    }

    public Task<(Stream Content, string FileName, string ContentType)> DownloadFileAsync(
        string accessToken, string fileId, CancellationToken cancellationToken = default)
    {
        // TODO: service.Files.Get(fileId) then MediaDownloader
        throw new NotImplementedException("Install Google.Apis.Drive.v3 NuGet package and implement this method.");
    }
}
