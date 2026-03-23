using MediatR;
using UnifiedStorage.Application.Common.Interfaces;
using UnifiedStorage.Application.Files.Queries.GetFiles;
using UnifiedStorage.Domain.Exceptions;

namespace UnifiedStorage.Application.Files.Commands.UploadFile;

public class UploadFileCommandHandler : IRequestHandler<UploadFileCommand, CloudFileDto>
{
    private readonly IStorageConnectionRepository _repository;
    private readonly IEnumerable<IStorageProviderService> _providerServices;
    private readonly ITokenEncryptionService _encryption;
    private readonly IOAuthService _oAuthService;
    private readonly ICurrentUserService _currentUser;

    public UploadFileCommandHandler(
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

    public async Task<CloudFileDto> Handle(UploadFileCommand request, CancellationToken cancellationToken)
    {
        var connection = await _repository.GetByUserAndProviderAsync(
            _currentUser.UserId, request.Provider, cancellationToken)
            ?? throw new StorageConnectionNotFoundException(_currentUser.UserId, request.Provider);

        var providerService = _providerServices.First(s => s.Provider == request.Provider);
        var accessToken = await GetValidAccessTokenAsync(connection, cancellationToken);

        var uploaded = await providerService.UploadFileAsync(
            accessToken, request.FileName, request.Content, request.FolderId, cancellationToken);

        connection.Touch();
        await _repository.UpdateAsync(connection, cancellationToken);

        return new CloudFileDto
        {
            Id = uploaded.Id,
            Name = uploaded.Name,
            SizeBytes = uploaded.SizeBytes,
            ModifiedAt = uploaded.ModifiedAt,
            Provider = uploaded.Provider,
            IsFolder = uploaded.IsFolder,
            MimeType = uploaded.MimeType,
            ParentId = uploaded.ParentId
        };
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
