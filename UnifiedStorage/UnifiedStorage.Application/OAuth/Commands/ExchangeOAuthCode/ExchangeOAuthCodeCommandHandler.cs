using MediatR;
using UnifiedStorage.Application.Common.Interfaces;
using UnifiedStorage.Domain.Entities;

namespace UnifiedStorage.Application.OAuth.Commands.ExchangeOAuthCode;

public class ExchangeOAuthCodeCommandHandler : IRequestHandler<ExchangeOAuthCodeCommand>
{
    private readonly IOAuthService _oAuthService;
    private readonly IStorageConnectionRepository _repository;
    private readonly ITokenEncryptionService _encryption;

    public ExchangeOAuthCodeCommandHandler(
        IOAuthService oAuthService,
        IStorageConnectionRepository repository,
        ITokenEncryptionService encryption)
    {
        _oAuthService = oAuthService;
        _repository = repository;
        _encryption = encryption;
    }

    public async Task Handle(ExchangeOAuthCodeCommand request, CancellationToken cancellationToken)
    {
        // Decode userId from state
        var stateBytes = Convert.FromBase64String(request.State);
        var stateStr = System.Text.Encoding.UTF8.GetString(stateBytes);
        var userId = stateStr.Split(':')[0];

        var tokenResult = await _oAuthService.ExchangeCodeForTokensAsync(
            request.Provider, request.Code, request.RedirectUri, cancellationToken);

        var encryptedAccess = _encryption.Encrypt(tokenResult.AccessToken);
        var encryptedRefresh = _encryption.Encrypt(tokenResult.RefreshToken);

        var existing = await _repository.GetByUserAndProviderAsync(userId, request.Provider, cancellationToken);

        if (existing is not null)
        {
            existing.UpdateTokens(encryptedAccess, encryptedRefresh, tokenResult.ExpiresAt);
            await _repository.UpdateAsync(existing, cancellationToken);
        }
        else
        {
            var connection = StorageConnection.Create(
                userId, request.Provider, encryptedAccess, encryptedRefresh, tokenResult.ExpiresAt);
            await _repository.AddAsync(connection, cancellationToken);
        }
    }
}
