using UnifiedStorage.Domain.Enums;
using UnifiedStorage.Domain.Models;

namespace UnifiedStorage.Application.Common.Interfaces;

public interface IStorageProviderService
{
    StorageProvider Provider { get; }

    Task<IReadOnlyList<CloudFile>> ListFilesAsync(
        string accessToken,
        string? folderId,
        CancellationToken cancellationToken = default);

    Task<CloudFile> UploadFileAsync(
        string accessToken,
        string fileName,
        Stream content,
        string? folderId,
        CancellationToken cancellationToken = default);

    Task DeleteFileAsync(
        string accessToken,
        string fileId,
        CancellationToken cancellationToken = default);

    Task<(Stream Content, string FileName, string ContentType)> DownloadFileAsync(
        string accessToken,
        string fileId,
        CancellationToken cancellationToken = default);
}
