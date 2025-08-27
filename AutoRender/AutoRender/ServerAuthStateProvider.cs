using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using Yardify.Frontend.Client.Interfaces.Authentication;

namespace AutoRender;

public class ServerAuthStateProvider : AuthenticationStateProvider, IYardifyAuthenticationService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IYardifyAuthenticationService _authService;

    public ServerAuthStateProvider(IHttpContextAccessor httpContextAccessor, IYardifyAuthenticationService authService)
    {
        _httpContextAccessor = httpContextAccessor;
        _authService = authService;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            return Task.FromResult(new AuthenticationState(httpContext.User));
        }

        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }

    // Delegate IYardifyAuthenticationService methods
    public Task<bool> LoginAsync(LoginRequestDTO loginRequest) => _authService.LoginAsync(loginRequest);
    public Task<bool> LogoutAsync() => _authService.LogoutAsync();
    public Task<UserDetailDTO?> GetCurrentUserInfoAsync() => _authService.GetCurrentUserInfoAsync();
}
