using BlazorApp1.Client.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using System.Net;
using System.Security.Claims;

namespace BlazorApp1.Services;

public class ServerAuthService : IAuthService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ServerAuthService> _logger;

    public ServerAuthService(
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ServerAuthService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<bool> LoginAsync(string email, string password)
    {
        try
        {
            // Use the configured HttpClient from factory
            using var client = _httpClientFactory.CreateClient("WebApi");

            var loginRequest = new { email, password };
            var response = await client.PostAsJsonAsync(
                "login?useCookies=true&useSessionCookies=false",
                loginRequest);

            if (response.IsSuccessStatusCode)
            {
                // Create local authentication session in Blazor app
                if (_httpContextAccessor.HttpContext != null &&
                    !_httpContextAccessor.HttpContext.Response.HasStarted)
                {
                    var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, email),
                            new Claim(ClaimTypes.Email, email)
                        };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddSeconds(10),
                        // Store last activity time for inactivity tracking
                        Items = { ["LastActivity"] = DateTimeOffset.UtcNow.ToString("o") }
                    };

                    // Sign in to create local authentication cookie
                    await _httpContextAccessor.HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);
                }

                return true;
            }

            _logger.LogWarning("Login failed for user {Email}. Status: {StatusCode}",
                email, response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user {Email}", email);
            return false;
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            // Sign out locally first
            if (_httpContextAccessor.HttpContext != null)
            {
                await _httpContextAccessor.HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);
            }

            // Then logout from API to clear server-side cookie
            var client = _httpClientFactory.CreateClient("WebApi");
            var response = await client.PostAsync("logout", null);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("API logout returned status {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            // Still consider logout successful even if API call fails
        }
    }

    public Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            return Task.FromResult(new AuthenticationState
            {
                IsAuthenticated = true,
                UserName = httpContext.User.Identity.Name
            });
        }

        return Task.FromResult(new AuthenticationState { IsAuthenticated = false });
    }
}