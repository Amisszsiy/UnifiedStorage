using MediatR;
using UnifiedStorage.Application.Common.Interfaces;
using UnifiedStorage.Domain.Enums;
using UnifiedStorage.Domain.Exceptions;

namespace UnifiedStorage.Application.Files.Queries.GetFiles;

public class GetFilesQueryHandler : IRequestHandler<GetFilesQuery, IReadOnlyList<CloudFileDto>>
{
    private readonly IStorageConnectionRepository _repository;
    private readonly IEnumerable<IStorageProviderService> _providerServices;
    private readonly ITokenEncryptionService _encryption;
    private readonly IOAuthService _oAuthService;
    private readonly ICurrentUserService _currentUser;

    public GetFilesQueryHandler(
        IStorageConnectionRepository repository,
        IEnumerable<IStorageProviderService> providerServices,
        ITokenEncryptionService encryption,
        IOAuthService oAuthService,
        ICurrentUserService currentUser)
    {
        _repository = repository;
        _providerServices = providerServices;
        _encryption = encryption;
        _oAuthService = oAuthService;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<CloudFileDto>> Handle(
        GetFilesQuery request,
        CancellationToken cancellationToken)
    {
        var connections = await _repository.GetAllByUserAsync(_currentUser.UserId, cancellationToken);

        if (request.Provider.HasValue)
            connections = connections.Where(c => c.Provider == request.Provider.Value).ToList();

        connections = connections.Where(c => c.Status == ConnectionStatus.Active).ToList();

        var tasks = connections.Select(conn => FetchFilesForConnectionAsync(conn, request.FolderId, cancellationToken));
        var results = await Task.WhenAll(tasks);

        return results.SelectMany(f => f).ToList();
    }

    private async Task<IReadOnlyList<CloudFileDto>> FetchFilesForConnectionAsync(
        Domain.Entities.StorageConnection connection,
        string? folderId,
        CancellationToken cancellationToken)
    {
        var providerService = _providerServices.FirstOrDefault(s => s.Provider == connection.Provider);
        if (providerService is null) return [];

        try
        {
            var accessToken = await GetValidAccessTokenAsync(connection, cancellationToken);
            var files = await providerService.ListFilesAsync(accessToken, folderId, cancellationToken);

            connection.Touch();
            await _repository.UpdateAsync(connection, cancellationToken);

            return files.Select(f => new CloudFileDto
            {
                Id = f.Id,
                Name = f.Name,
                SizeBytes = f.SizeBytes,
                ModifiedAt = f.ModifiedAt,
                Provider = f.Provider,
                IsFolder = f.IsFolder,
                MimeType = f.MimeType,
                ParentId = f.ParentId
            }).ToList();
        }
        catch (ReAuthRequiredException)
        {
            connection.MarkAsExpired();
            await _repository.UpdateAsync(connection, cancellationToken);
            return [];
        }
    }

    private async Task<string> GetValidAccessTokenAsync(
        Domain.Entities.StorageConnection connection,
        CancellationToken cancellationToken)
    {
        if (!connection.IsAccessTokenExpiringSoon())
            return _encryption.Decrypt(connection.EncryptedAccessToken);

        try
        {
            var refreshToken = _encryption.Decrypt(connection.EncryptedRefreshToken);
            var result = await _oAuthService.RefreshTokenAsync(connection.Provider, refreshToken, cancellationToken);

            connection.UpdateTokens(
                _encryption.Encrypt(result.AccessToken),
                _encryption.Encrypt(result.RefreshToken),
                result.ExpiresAt);

            await _repository.UpdateAsync(connection, cancellationToken);
            return result.AccessToken;
        }
        catch
        {
            throw new ReAuthRequiredException(connection.Provider);
        }
    }
}
