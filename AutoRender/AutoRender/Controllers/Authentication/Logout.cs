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
        HttpContext.Session.Clear();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return Ok(new { success = true });
    }
}