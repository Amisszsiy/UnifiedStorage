using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions.Authentication;
using UnifiedStorage.Application.Common.Interfaces;
using UnifiedStorage.Domain.Enums;
using UnifiedStorage.Domain.Models;

namespace UnifiedStorage.Infrastructure.Providers;

public class OneDriveService : IStorageProviderService
{
    public StorageProvider Provider => StorageProvider.OneDrive;

    private static GraphServiceClient CreateGraphClient(string accessToken) =>
        new(new BaseBearerTokenAuthenticationProvider(new StaticAccessTokenProvider(accessToken)));

    private static async Task<string> GetDriveIdAsync(GraphServiceClient client, CancellationToken ct)
    {
        var drive = await client.Me.Drive.GetAsync(cancellationToken: ct);
        return drive?.Id ?? throw new InvalidOperationException("Could not retrieve OneDrive drive ID.");
    }

    public async Task<IReadOnlyList<CloudFile>> ListFilesAsync(
        string accessToken, string? folderId, CancellationToken cancellationToken = default)
    {
        var client = CreateGraphClient(accessToken);
        var driveId = await GetDriveIdAsync(client, cancellationToken);

        var itemId = folderId ?? "root";
        var page = await client.Drives[driveId].Items[itemId].Children
            .GetAsync(cancellationToken: cancellationToken);

        var files = new List<CloudFile>();

        while (page?.Value != null)
        {
            files.AddRange(page.Value.Select(MapToCloudFile));

            if (page.OdataNextLink is null) break;

            page = await client.Drives[driveId].Items[itemId].Children
                .WithUrl(page.OdataNextLink)
                .GetAsync(cancellationToken: cancellationToken);
        }

        return files;
    }

    public async Task<CloudFile> UploadFileAsync(
        string accessToken, string fileName, Stream content, string? folderId,
        CancellationToken cancellationToken = default)
    {
        var client = CreateGraphClient(accessToken);
        var driveId = await GetDriveIdAsync(client, cancellationToken);

        var parentId = folderId ?? "root";
        var uploaded = await client.Drives[driveId]
            .Items[parentId]
            .ItemWithPath(fileName)
            .Content
            .PutAsync(content, cancellationToken: cancellationToken);

        return MapToCloudFile(uploaded!);
    }

    public async Task DeleteFileAsync(
        string accessToken, string fileId, CancellationToken cancellationToken = default)
    {
        var client = CreateGraphClient(accessToken);
        var driveId = await GetDriveIdAsync(client, cancellationToken);

        await client.Drives[driveId].Items[fileId].DeleteAsync(cancellationToken: cancellationToken);
    }

    public async Task<(Stream Content, string FileName, string ContentType)> DownloadFileAsync(
        string accessToken, string fileId, CancellationToken cancellationToken = default)
    {
        var client = CreateGraphClient(accessToken);
        var driveId = await GetDriveIdAsync(client, cancellationToken);

        var meta = await client.Drives[driveId].Items[fileId]
            .GetAsync(cancellationToken: cancellationToken);

        var stream = await client.Drives[driveId].Items[fileId].Content
            .GetAsync(cancellationToken: cancellationToken);

        var memStream = new MemoryStream();
        await stream!.CopyToAsync(memStream, cancellationToken);
        memStream.Position = 0;

        return (memStream, meta?.Name ?? fileId, meta?.File?.MimeType ?? "application/octet-stream");
    }

    private static CloudFile MapToCloudFile(DriveItem item) => new()
    {
        Id = item.Id!,
        Name = item.Name!,
        SizeBytes = item.Size ?? 0,
        ModifiedAt = item.LastModifiedDateTime?.UtcDateTime,
        Provider = StorageProvider.OneDrive,
        DownloadUrl = item.AdditionalData.TryGetValue("@microsoft.graph.downloadUrl", out var url)
            ? url?.ToString()
            : null,
        IsFolder = item.Folder != null,
        MimeType = item.File?.MimeType,
        ParentId = item.ParentReference?.Id
    };
}

file sealed class StaticAccessTokenProvider(string accessToken) : IAccessTokenProvider
{
    public Task<string> GetAuthorizationTokenAsync(
        Uri uri,
        Dictionary<string, object>? additionalAuthenticationContext = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(accessToken);

    public AllowedHostsValidator AllowedHostsValidator { get; } = new();
}
