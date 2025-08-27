namespace Yardify.Frontend.Client.Interfaces.Authentication;

public interface IYardifyAuthenticationService
{
    Task<bool> LoginAsync(LoginRequestDTO loginRequest);
    Task<bool> LogoutAsync();
    Task<UserDetailDTO?> GetCurrentUserInfoAsync();
}

public class LoginRequestDTO
{
    public string? Email { get; set; }
    public string? Password { get; set; }
}

public class LoginResponseDTO
{
    public string TokenType { get; set; } = "";
    public string AccessToken { get; set; } = "";
    public long ExpiresIn { get; set; }
    public string RefreshToken { get; set; } = "";
}

public class UserDetailDTO
{
    public string? Email { get; set; }
    public string? Role { get; set; }
    public bool IsAuthenticated { get; set; }
}
