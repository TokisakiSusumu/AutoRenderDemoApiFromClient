namespace BlazorApp1.Services
{
    public interface IAuthService
    {
        Task<bool> LoginAsync(string email, string password);
        Task LogoutAsync();
        Task<AuthenticationState> GetAuthenticationStateAsync();
    }

    public class AuthenticationState
    {
        public bool IsAuthenticated { get; set; }
        public string? UserName { get; set; }
    }
}