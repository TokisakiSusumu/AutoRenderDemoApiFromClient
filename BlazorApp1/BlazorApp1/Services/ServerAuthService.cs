using BlazorApp1.Client.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;

namespace BlazorApp1.Services
{
    public class ServerAuthService : IAuthService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly CookieContainer _cookieContainer;

        public ServerAuthService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
            _cookieContainer = new CookieContainer();
        }

        public async Task<bool> LoginAsync(string email, string password)
        {
            using var handler = new HttpClientHandler
            {
                CookieContainer = _cookieContainer,
                UseCookies = true
            };

            using var client = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://localhost:7191/")
            };

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
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14)
                    };

                    // Sign in to create local authentication cookie
                    await _httpContextAccessor.HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);
                }

                return true;
            }

            return false;
        }

        public async Task LogoutAsync()
        {
            // Sign out locally
            if (_httpContextAccessor.HttpContext != null)
            {
                await _httpContextAccessor.HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);
            }

            // Also logout from API
            using var handler = new HttpClientHandler
            {
                CookieContainer = _cookieContainer,
                UseCookies = true
            };

            using var client = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://localhost:7191/")
            };

            await client.PostAsync("logout", null);
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
}