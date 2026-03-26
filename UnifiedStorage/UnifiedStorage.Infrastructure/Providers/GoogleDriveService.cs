using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Upload;
using UnifiedStorage.Application.Common.Interfaces;
using UnifiedStorage.Domain.Enums;
using UnifiedStorage.Domain.Models;

namespace UnifiedStorage.Infrastructure.Providers;

public class GoogleDriveService : IStorageProviderService
{
    public StorageProvider Provider => StorageProvider.GoogleDrive;

    private DriveService CreateDriveService(string accessToken)
    {
        var credential = GoogleCredential.FromAccessToken(accessToken);
        return new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "UnifiedStorage"
        });
    }

    public async Task<IReadOnlyList<CloudFile>> ListFilesAsync(
        string accessToken, string? folderId, CancellationToken cancellationToken = default)
    {
        using var service = CreateDriveService(accessToken);

        var request = service.Files.List();
        request.Fields = "files(id,name,size,modifiedTime,mimeType,parents,webContentLink)";
        request.PageSize = 1000;

        if (folderId != null)
            request.Q = $"'{folderId}' in parents and trashed = false";
        else
            request.Q = "'root' in parents and trashed = false";

        var result = await request.ExecuteAsync(cancellationToken);

        return result.Files.Select(f => new CloudFile
        {
            Id = f.Id,
            Name = f.Name,
            SizeBytes = f.Size ?? 0,
            ModifiedAt = f.ModifiedTimeDateTimeOffset?.UtcDateTime,
            Provider = StorageProvider.GoogleDrive,
            DownloadUrl = f.WebContentLink,
            IsFolder = f.MimeType == "application/vnd.google-apps.folder",
            MimeType = f.MimeType,
            ParentId = f.Parents?.FirstOrDefault()
        }).ToList();
    }

    public async Task<CloudFile> UploadFileAsync(
        string accessToken, string fileName, Stream content, string? folderId,
        CancellationToken cancellationToken = default)
    {
        using var service = CreateDriveService(accessToken);

        var fileMetadata = new Google.Apis.Drive.v3.Data.File { Name = fileName };
        if (folderId != null)
            fileMetadata.Parents = [folderId];

        var request = service.Files.Create(fileMetadata, content, "application/octet-stream");
        request.Fields = "id,name,size,modifiedTime,mimeType,parents,webContentLink";

        var uploadProgress = await request.UploadAsync(cancellationToken);
        if (uploadProgress.Status != UploadStatus.Completed)
            throw new InvalidOperationException($"Upload failed: {uploadProgress.Exception?.Message}");

        var uploaded = request.ResponseBody;
        return new CloudFile
        {
            Id = uploaded.Id,
            Name = uploaded.Name,
            SizeBytes = uploaded.Size ?? 0,
            ModifiedAt = uploaded.ModifiedTimeDateTimeOffset?.UtcDateTime,
            Provider = StorageProvider.GoogleDrive,
            DownloadUrl = uploaded.WebContentLink,
            IsFolder = false,
            MimeType = uploaded.MimeType,
            ParentId = uploaded.Parents?.FirstOrDefault()
        };
    }

    public async Task DeleteFileAsync(
        string accessToken, string fileId, CancellationToken cancellationToken = default)
    {
        using var service = CreateDriveService(accessToken);
        await service.Files.Delete(fileId).ExecuteAsync(cancellationToken);
    }

    public async Task<(Stream Content, string FileName, string ContentType)> DownloadFileAsync(
        string accessToken, string fileId, CancellationToken cancellationToken = default)
    {
        using var service = CreateDriveService(accessToken);

        var metaRequest = service.Files.Get(fileId);
        metaRequest.Fields = "id,name,mimeType";
        var file = await metaRequest.ExecuteAsync(cancellationToken);

        // Google Docs native formats cannot be downloaded directly — export as PDF
        string contentType;
        Stream responseStream;

        if (file.MimeType.StartsWith("application/vnd.google-apps."))
        {
            contentType = "application/pdf";
            var exportRequest = service.Files.Export(fileId, contentType);
            var memStream = new MemoryStream();
            await exportRequest.DownloadAsync(memStream, cancellationToken);
            memStream.Position = 0;
            responseStream = memStream;
        }
        else
        {
            contentType = file.MimeType ?? "application/octet-stream";
            var downloadRequest = service.Files.Get(fileId);
            var memStream = new MemoryStream();
            await downloadRequest.DownloadAsync(memStream, cancellationToken);
            memStream.Position = 0;
            responseStream = memStream;
        }

        return (responseStream, file.Name, contentType);
    }
}
