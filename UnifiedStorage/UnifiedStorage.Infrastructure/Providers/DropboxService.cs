using Dropbox.Api;
using Dropbox.Api.Files;
using UnifiedStorage.Application.Common.Interfaces;
using UnifiedStorage.Domain.Enums;
using UnifiedStorage.Domain.Models;

namespace UnifiedStorage.Infrastructure.Providers;

public class DropboxService : IStorageProviderService
{
    public StorageProvider Provider => StorageProvider.Dropbox;

    public async Task<IReadOnlyList<CloudFile>> ListFilesAsync(
        string accessToken, string? folderId, CancellationToken cancellationToken = default)
    {
        using var client = new DropboxClient(accessToken);

        var result = await client.Files.ListFolderAsync(folderId ?? string.Empty);
        var entries = new List<Metadata>(result.Entries);

        while (result.HasMore)
        {
            result = await client.Files.ListFolderContinueAsync(result.Cursor);
            entries.AddRange(result.Entries);
        }

        return entries.Select(MapToCloudFile).ToList();
    }

    public async Task<CloudFile> UploadFileAsync(
        string accessToken, string fileName, Stream content, string? folderId,
        CancellationToken cancellationToken = default)
    {
        using var client = new DropboxClient(accessToken);

        var path = string.IsNullOrEmpty(folderId)
            ? $"/{fileName}"
            : $"{folderId.TrimEnd('/')}/{fileName}";

        var uploaded = await client.Files.UploadAsync(
            path,
            WriteMode.Overwrite.Instance,
            body: content);

        return MapToCloudFile(uploaded);
    }

    public async Task DeleteFileAsync(
        string accessToken, string fileId, CancellationToken cancellationToken = default)
    {
        using var client = new DropboxClient(accessToken);
        await client.Files.DeleteV2Async(fileId);
    }

    public async Task<(Stream Content, string FileName, string ContentType)> DownloadFileAsync(
        string accessToken, string fileId, CancellationToken cancellationToken = default)
    {
        using var client = new DropboxClient(accessToken);

        var response = await client.Files.DownloadAsync(fileId);
        var stream = await response.GetContentAsStreamAsync();

        var memStream = new MemoryStream();
        await stream.CopyToAsync(memStream);
        memStream.Position = 0;

        return (memStream, response.Response.Name, "application/octet-stream");
    }

    private static CloudFile MapToCloudFile(Metadata entry)
    {
        if (entry is FileMetadata file)
        {
            return new CloudFile
            {
                Id = file.PathDisplay ?? file.PathLower ?? file.Name,
                Name = file.Name,
                SizeBytes = (long)file.Size,
                ModifiedAt = file.ServerModified,
                Provider = StorageProvider.Dropbox,
                IsFolder = false,
                MimeType = null,
                ParentId = GetParentPath(file.PathDisplay)
            };
        }

        if (entry is FolderMetadata folder)
        {
            return new CloudFile
            {
                Id = folder.PathDisplay ?? folder.PathLower ?? folder.Name,
                Name = folder.Name,
                SizeBytes = 0,
                ModifiedAt = null,
                Provider = StorageProvider.Dropbox,
                IsFolder = true,
                MimeType = null,
                ParentId = GetParentPath(folder.PathDisplay)
            };
        }

        // DeletedMetadata — shouldn't appear in normal listing but handle gracefully
        return new CloudFile
        {
            Id = entry.PathDisplay ?? entry.Name,
            Name = entry.Name,
            Provider = StorageProvider.Dropbox
        };
    }

    private static string? GetParentPath(string? path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        var lastSlash = path.LastIndexOf('/');
        return lastSlash <= 0 ? null : path[..lastSlash];
    }
}
