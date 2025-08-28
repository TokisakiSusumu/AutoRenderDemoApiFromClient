using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace AutoRender;

public class ServerAuthStateProvider : AuthenticationStateProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ServerAuthStateProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            // Check if token is still valid
            var expirationStr = httpContext.Session.GetString("TokenExpiration");
            if (!string.IsNullOrEmpty(expirationStr))
            {
                if (DateTimeOffset.TryParse(expirationStr, out var expiration))
                {
                    if (expiration <= DateTimeOffset.UtcNow)
                    {
                        // Token expired - return anonymous
                        return Task.FromResult(new AuthenticationState(
                            new ClaimsPrincipal(new ClaimsIdentity())));
                    }
                }
            }

            return Task.FromResult(new AuthenticationState(httpContext.User));
        }

        return Task.FromResult(new AuthenticationState(
            new ClaimsPrincipal(new ClaimsIdentity())));
    }
}