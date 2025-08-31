using BlazorApp1.Services;
using Microsoft.AspNetCore.Mvc;

namespace TestAuthAuto.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // Handle form POST from login page
        [HttpPost("login-form")]
        public async Task<IActionResult> LoginForm(
            [FromForm] string email,
            [FromForm] string password,
            [FromForm] string? returnUrl)
        {
            var success = await _authService.LoginAsync(email, password);

            if (success)
            {
                // Authentication cookie is already set by ServerAuthService
                return Redirect(returnUrl ?? "/");
            }

            return Redirect("/login?error=true");
        }

        // Handle logout
        [HttpPost("logout-form")]
        public async Task<IActionResult> LogoutForm()
        {
            await _authService.LogoutAsync();
            return Redirect("/");
        }
    }

    public class LoginRequest
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }
}