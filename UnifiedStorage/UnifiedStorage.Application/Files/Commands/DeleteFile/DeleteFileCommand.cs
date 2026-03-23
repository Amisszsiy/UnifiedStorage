using MediatR;
using UnifiedStorage.Domain.Enums;

namespace UnifiedStorage.Application.Files.Commands.DeleteFile;

public record DeleteFileCommand(StorageProvider Provider, string FileId) : IRequest;
