using MediatR;
using UnifiedStorage.Domain.Enums;

namespace UnifiedStorage.Application.StorageConnections.Commands.DisconnectStorage;

public record DisconnectStorageCommand(StorageProvider Provider) : IRequest;
