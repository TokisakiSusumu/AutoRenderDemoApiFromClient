using System.Net.Http.Json;

namespace BlazorApp1.Client.Services
{
    public class ClientAuthService : IAuthService
    {
        private readonly HttpClient _httpClient;

        public ClientAuthService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<bool> LoginAsync(string email, string password)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/login", new { email, password });
            return response.IsSuccessStatusCode;
        }

        public async Task LogoutAsync()
        {
            await _httpClient.PostAsync("/api/auth/logout", null);
        }

        public async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<AuthenticationState>("/api/auth/state")
                    ?? new AuthenticationState();
            }
            catch
            {
                return new AuthenticationState();
            }
        }
    }
}