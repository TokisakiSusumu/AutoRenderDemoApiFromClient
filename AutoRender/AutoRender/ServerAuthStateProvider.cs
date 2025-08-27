using AutoRender.Client;
using AutoRender.Client.Models;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using Yardify.Frontend.Client.Interfaces.Authentication;

namespace AutoRender;

public class ServerAuthStateProvider(IYardifyAuthenticationService authService, IHttpContextAccessor httpContextAccessor) : AuthenticationStateProvider
{
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var httpContext = httpContextAccessor.HttpContext;

        // Check if user is authenticated via cookie
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            // User is authenticated, return the existing principal
            return new AuthenticationState(httpContext.User);
        }

        // Not authenticated
        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
    }
}