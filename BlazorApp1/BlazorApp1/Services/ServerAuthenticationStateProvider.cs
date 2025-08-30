using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Components.Web;
using System.Security.Claims;
using BlazorApp1.Client.Services; // Import UserInfo from Client project

namespace BlazorApp1.Services
{
    public class ServerAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly PersistentComponentState _state;
        private readonly PersistingComponentStateSubscription _subscription;

        public ServerAuthenticationStateProvider(
            IHttpContextAccessor httpContextAccessor,
            PersistentComponentState persistentComponentState)
        {
            _httpContextAccessor = httpContextAccessor;
            _state = persistentComponentState;
            _subscription = _state.RegisterOnPersisting(OnPersistingAsync, RenderMode.InteractiveWebAssembly);
        }

        public override Task<Microsoft.AspNetCore.Components.Authorization.AuthenticationState> GetAuthenticationStateAsync()
        {
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                return Task.FromResult(
                    new Microsoft.AspNetCore.Components.Authorization.AuthenticationState(httpContext.User));
            }

            return Task.FromResult(
                new Microsoft.AspNetCore.Components.Authorization.AuthenticationState(
                    new ClaimsPrincipal(new ClaimsIdentity())));
        }

        private Task OnPersistingAsync()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                var email = httpContext.User.Identity.Name ?? "";
                _state.PersistAsJson(nameof(UserInfo), new UserInfo { Email = email });
            }

            return Task.CompletedTask;
        }
    }
}