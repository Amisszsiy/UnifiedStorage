using MediatR;
using UnifiedStorage.Application.Common.Interfaces;

namespace UnifiedStorage.Application.StorageConnections.Queries.GetConnections;

public class GetConnectionsQueryHandler
    : IRequestHandler<GetConnectionsQuery, IReadOnlyList<StorageConnectionDto>>
{
    private readonly IStorageConnectionRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public GetConnectionsQueryHandler(
        IStorageConnectionRepository repository,
        ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<StorageConnectionDto>> Handle(
        GetConnectionsQuery request,
        CancellationToken cancellationToken)
    {
        var connections = await _repository.GetAllByUserAsync(_currentUser.UserId, cancellationToken);

        return connections.Select(c => new StorageConnectionDto
        {
            Id = c.Id,
            Provider = c.Provider,
            Status = c.Status,
            LastUsedAt = c.LastUsedAt,
            CreatedAt = c.CreatedAt,
            IsExpiringSoon = c.IsAccessTokenExpiringSoon()
        }).ToList();
    }
}
