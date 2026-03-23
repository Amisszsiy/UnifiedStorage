using MediatR;
using UnifiedStorage.Domain.Enums;

namespace UnifiedStorage.Application.Files.Queries.GetFiles;

public record GetFilesQuery(
    StorageProvider? Provider = null,
    string? FolderId = null) : IRequest<IReadOnlyList<CloudFileDto>>;
