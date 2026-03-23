using MediatR;
using UnifiedStorage.Domain.Enums;

namespace UnifiedStorage.Application.Files.Queries.DownloadFile;

public record DownloadFileQuery(StorageProvider Provider, string FileId)
    : IRequest<DownloadFileResult>;
