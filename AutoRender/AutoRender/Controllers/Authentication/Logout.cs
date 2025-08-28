using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Yardify.Frontend.Components.Authentication.Controllers.Authentication;

public sealed partial class YardifyAuthenticationController : ControllerBase
{
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        // Clear all session data
        HttpContext.Session.Clear();

        // Sign out from cookie authentication
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        // If it's a form post (from server-side), redirect
        if (!HttpContext.Request.Headers["Content-Type"].ToString().Contains("application/json"))
        {
            return LocalRedirect("~/");
        }

        return Ok(new { success = true });
    }
}