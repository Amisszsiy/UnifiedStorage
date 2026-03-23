using MediatR;

namespace UnifiedStorage.Application.StorageConnections.Queries.GetConnections;

public record GetConnectionsQuery : IRequest<IReadOnlyList<StorageConnectionDto>>;
