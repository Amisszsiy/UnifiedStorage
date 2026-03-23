using MediatR;
using UnifiedStorage.Domain.Enums;

namespace UnifiedStorage.Application.OAuth.Queries.GetAuthorizationUrl;

public record GetAuthorizationUrlQuery(StorageProvider Provider, string RedirectUri) : IRequest<string>;
