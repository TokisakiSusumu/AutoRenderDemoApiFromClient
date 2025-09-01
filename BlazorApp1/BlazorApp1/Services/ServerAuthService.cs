using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.Net;

namespace BlazorApp1.Services
{
    public class ServerAuthService(
        IHttpContextAccessor httpContextAccessor,
        WebApiHttpClient webApiHttpClient,
        ILogger<ServerAuthService> logger) : IAuthService
    {
        public async Task<bool> LoginAsync(string email, string password)
        {
            try
            {
                var httpContext = httpContextAccessor.HttpContext;
                if (httpContext == null)
                {
                    logger.LogError("No HttpContext available for login");
                    return false;
                }

                // Clean up any existing session
                await LogoutAsync();

                // Use WebApiHttpClient for login
                var response = await webApiHttpClient.LoginAsync(email, password);

                if (response.IsSuccessStatusCode)
                {
                    // Get the cookie value from response headers
                    if (response.Headers.TryGetValues("X-API-Cookie-Value", out var cookieValues))
                    {
                        var apiCookieValue = cookieValues.FirstOrDefault();

                        if (!string.IsNullOrEmpty(apiCookieValue) && !httpContext.Response.HasStarted)
                        {
                            // Get expiration from API response
                            string? authExpires = null;
                            if (response.Headers.TryGetValues("X-Auth-Expires", out var expiresValues))
                            {
                                authExpires = expiresValues.FirstOrDefault();
                            }

                            // Forward the API cookie to browser
                            httpContext.Response.Cookies.Append(
                                ".AspNetCore.Identity.Application",
                                apiCookieValue,
                                new CookieOptions
                                {
                                    HttpOnly = true,
                                    Secure = true,
                                    SameSite = SameSiteMode.Lax
                                });

                            // Create Blazor authentication
                            var claims = new List<Claim>
                            {
                                new Claim(ClaimTypes.Name, email),
                                new Claim(ClaimTypes.Email, email),
                                new Claim("SessionId", Guid.NewGuid().ToString())
                            };

                            if (!string.IsNullOrEmpty(authExpires))
                            {
                                claims.Add(new Claim("ApiAuthExpires", authExpires));
                            }

                            var claimsIdentity = new ClaimsIdentity(
                                claims,
                                CookieAuthenticationDefaults.AuthenticationScheme);

                            var authProperties = new AuthenticationProperties
                            {
                                IsPersistent = true,
                                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1)
                            };

                            await httpContext.SignInAsync(
                                CookieAuthenticationDefaults.AuthenticationScheme,
                                new ClaimsPrincipal(claimsIdentity),
                                authProperties);

                            logger.LogInformation("Login successful for {Email}", email);
                            return true;
                        }
                    }
                }

                logger.LogWarning("Login failed for {Email}. Status: {StatusCode}",
                    email, response.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during login for {Email}", email);
                return false;
            }
        }

        public async Task LogoutAsync()
        {
            try
            {
                var httpContext = httpContextAccessor.HttpContext;
                if (httpContext == null) return;

                // Use WebApiHttpClient for API logout
                await webApiHttpClient.LogoutAsync();

                // Clear cookies
                httpContext.Response.Cookies.Delete(".AspNetCore.Identity.Application");
                httpContext.Response.Cookies.Delete("BlazorAuthCookie");

                // Sign out from Blazor
                await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                logger.LogInformation("User logged out successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during logout");
            }
        }

        public Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var httpContext = httpContextAccessor.HttpContext;

            if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                return Task.FromResult(new AuthenticationState
                {
                    IsAuthenticated = true,
                    UserName = httpContext.User.Identity.Name
                });
            }

            return Task.FromResult(new AuthenticationState
            {
                IsAuthenticated = false
            });
        }
    }
}