namespace AutoRender.Client.Models;

public class LoginRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public class LoginResponse
{
    public string TokenType { get; set; } = "";
    public string AccessToken { get; set; } = "";
    public long ExpiresIn { get; set; }
    public string RefreshToken { get; set; } = "";
}
public class UserInfo
{
    public string Email { get; set; } = "";
    public string Role { get; set; }
    public bool IsAuthenticated { get; set; }
}