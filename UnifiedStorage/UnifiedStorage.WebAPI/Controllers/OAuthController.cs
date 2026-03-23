using MediatR;
using Microsoft.AspNetCore.Mvc;
using UnifiedStorage.Application.OAuth.Commands.ExchangeOAuthCode;
using UnifiedStorage.Application.OAuth.Queries.GetAuthorizationUrl;
using UnifiedStorage.Domain.Enums;

namespace UnifiedStorage.WebAPI.Controllers;

[ApiController]
[Route("api/oauth")]
public class OAuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public OAuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Returns the OAuth authorization URL for the given provider.
    /// The frontend should redirect the user to this URL.
    /// </summary>
    [HttpGet("connect/{provider}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> Connect(
        [FromRoute] StorageProvider provider,
        CancellationToken cancellationToken)
    {
        var redirectUri = BuildCallbackUri(provider);
        var url = await _mediator.Send(new GetAuthorizationUrlQuery(provider, redirectUri), cancellationToken);
        return Ok(new { authorizationUrl = url });
    }

    /// <summary>
    /// OAuth callback endpoint. The provider redirects here after user consent.
    /// Exchanges the authorization code for tokens and stores them.
    /// </summary>
    [HttpGet("callback/{provider}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Callback(
        [FromRoute] StorageProvider provider,
        [FromQuery] string code,
        [FromQuery] string state,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            return BadRequest(new { error = "Missing code or state parameter." });

        var redirectUri = BuildCallbackUri(provider);
        await _mediator.Send(new ExchangeOAuthCodeCommand(provider, code, state, redirectUri), cancellationToken);

        return Ok(new { message = $"{provider} connected successfully." });
    }

    private string BuildCallbackUri(StorageProvider provider)
    {
        var providerSlug = provider.ToString().ToLowerInvariant();
        return $"{Request.Scheme}://{Request.Host}/api/oauth/callback/{providerSlug}";
    }
}
