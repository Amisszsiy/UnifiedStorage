using MediatR;
using UnifiedStorage.Application.Common.Interfaces;
using UnifiedStorage.Domain.Exceptions;

namespace UnifiedStorage.Application.StorageConnections.Commands.DisconnectStorage;

public class DisconnectStorageCommandHandler : IRequestHandler<DisconnectStorageCommand>
{
    private readonly IStorageConnectionRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public DisconnectStorageCommandHandler(
        IStorageConnectionRepository repository,
        ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task Handle(DisconnectStorageCommand request, CancellationToken cancellationToken)
    {
        var connection = await _repository.GetByUserAndProviderAsync(
            _currentUser.UserId, request.Provider, cancellationToken);

        if (connection is null)
            throw new StorageConnectionNotFoundException(_currentUser.UserId, request.Provider);

        connection.MarkAsRevoked();
        await _repository.DeleteAsync(connection, cancellationToken);
    }
}
