using MediatR;
using UnifiedStorage.Domain.Enums;

namespace UnifiedStorage.Application.OAuth.Commands.ExchangeOAuthCode;

public record ExchangeOAuthCodeCommand(
    StorageProvider Provider,
    string Code,
    string State,
    string RedirectUri) : IRequest;
