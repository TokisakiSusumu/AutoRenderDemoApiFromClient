using AutoRender.Client.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Security.Claims;

namespace AutoRender.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthApiController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthApiController(IHttpClientFactory httpClientFactory, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.BaseAddress = new Uri(configuration["ApiBaseUrl"] ?? "https://localhost:7191/");
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
    {
        try
        {
            // Call the Identity API login endpoint directly
            var response = await _httpClient.PostAsJsonAsync("login", new
            {
                email = loginRequest.Email,
                password = loginRequest.Password
            });

            if (response.IsSuccessStatusCode)
            {
                var loginResponse = await response.Content.ReadFromJsonAsync<IdentityLoginResponse>();
                if (loginResponse != null)
                {
                    // Store tokens in session
                    HttpContext.Session.SetString("BearerToken", loginResponse.AccessToken);
                    HttpContext.Session.SetString("RefreshToken", loginResponse.RefreshToken);

                    // Create cookie authentication
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Email, loginRequest.Email),
                        new Claim(ClaimTypes.Name, loginRequest.Email)
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddSeconds(loginResponse.ExpiresIn)
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    return Ok(new { success = true });
                }
            }

            return Unauthorized(new { success = false, message = "Invalid credentials" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred during login" });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var token = HttpContext.Session.GetString("BearerToken");
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            await _httpClient.PostAsync("logout", null);
        }

        HttpContext.Session.Clear();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return Ok(new { success = true });
    }

    [HttpGet("current-user")]
    public async Task<ActionResult<UserInfo>> GetCurrentUser()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var token = HttpContext.Session.GetString("BearerToken");
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.GetAsync("manage/info");
                if (response.IsSuccessStatusCode)
                {
                    var identityInfo = await response.Content.ReadFromJsonAsync<IdentityUserInfo>();
                    if (identityInfo != null)
                    {
                        return Ok(new UserInfo
                        {
                            Email = identityInfo.Email,
                            Roles = Array.Empty<string>(),
                            IsAuthenticated = true
                        });
                    }
                }
            }
        }

        return Ok(new UserInfo { IsAuthenticated = false });
    }
}

// DTOs
public class IdentityLoginResponse
{
    public string TokenType { get; set; } = "";
    public string AccessToken { get; set; } = "";
    public int ExpiresIn { get; set; }
    public string RefreshToken { get; set; } = "";
}

public class IdentityUserInfo
{
    public string Email { get; set; } = "";
    public bool IsEmailConfirmed { get; set; }
}