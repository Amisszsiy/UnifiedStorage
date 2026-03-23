using MediatR;
using UnifiedStorage.Application.Common.Interfaces;

namespace UnifiedStorage.Application.OAuth.Queries.GetAuthorizationUrl;

public class GetAuthorizationUrlQueryHandler : IRequestHandler<GetAuthorizationUrlQuery, string>
{
    private readonly IOAuthService _oAuthService;
    private readonly ICurrentUserService _currentUser;

    public GetAuthorizationUrlQueryHandler(IOAuthService oAuthService, ICurrentUserService currentUser)
    {
        _oAuthService = oAuthService;
        _currentUser = currentUser;
    }

    public Task<string> Handle(GetAuthorizationUrlQuery request, CancellationToken cancellationToken)
    {
        // Encode userId in state for CSRF protection and user identification on callback
        var state = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes($"{_currentUser.UserId}:{Guid.NewGuid()}"));

        var url = _oAuthService.GetAuthorizationUrl(request.Provider, state, request.RedirectUri);
        return Task.FromResult(url);
    }
}
