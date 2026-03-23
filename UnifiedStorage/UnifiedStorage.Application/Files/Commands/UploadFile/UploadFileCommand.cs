using MediatR;
using UnifiedStorage.Application.Files.Queries.GetFiles;
using UnifiedStorage.Domain.Enums;

namespace UnifiedStorage.Application.Files.Commands.UploadFile;

public record UploadFileCommand(
    StorageProvider Provider,
    string FileName,
    Stream Content,
    string? FolderId = null) : IRequest<CloudFileDto>;
