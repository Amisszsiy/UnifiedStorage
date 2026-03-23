using System.Security.Claims;
using UnifiedStorage.Application.Common.Interfaces;

namespace UnifiedStorage.WebAPI.Services;

/// <summary>
/// Resolves the current user identity from JWT claims or the X-User-Id header (dev fallback).
/// Replace the header fallback with proper JWT authentication in production.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string UserId =>
        _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? _httpContextAccessor.HttpContext?.Request.Headers["X-User-Id"].FirstOrDefault()
        ?? throw new UnauthorizedAccessException("User is not authenticated.");
}
