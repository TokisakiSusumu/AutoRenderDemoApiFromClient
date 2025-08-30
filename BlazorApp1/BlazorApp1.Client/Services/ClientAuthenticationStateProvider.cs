using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace BlazorApp1.Client.Services
{
    public class ClientAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly PersistentComponentState _state;
        private readonly Task<Microsoft.AspNetCore.Components.Authorization.AuthenticationState> _authenticationStateTask;

        public ClientAuthenticationStateProvider(PersistentComponentState state)
        {
            _state = state;

            if (!_state.TryTakeFromJson<UserInfo>(nameof(UserInfo), out var userInfo) || userInfo is null)
            {
                _authenticationStateTask = Task.FromResult(
                    new Microsoft.AspNetCore.Components.Authorization.AuthenticationState(
                        new ClaimsPrincipal(new ClaimsIdentity())));
            }
            else
            {
                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, userInfo.Email),
                    new Claim(ClaimTypes.Email, userInfo.Email)
                };

                var identity = new ClaimsIdentity(claims, "serverauth");
                var user = new ClaimsPrincipal(identity);
                _authenticationStateTask = Task.FromResult(
                    new Microsoft.AspNetCore.Components.Authorization.AuthenticationState(user));
            }
        }

        public override Task<Microsoft.AspNetCore.Components.Authorization.AuthenticationState> GetAuthenticationStateAsync()
            => _authenticationStateTask;
    }
}