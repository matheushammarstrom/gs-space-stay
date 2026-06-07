namespace SpaceStay.Core.Abstractions;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string hash, string password);
}

public record TokenResult(string Token, DateTime ExpiresAt);

public interface ITokenService
{
    TokenResult GenerateToken(int userId, string email, string userType, string? role);
}

// Usuário autenticado, montado a partir das claims do JWT.
public record CurrentUser(int Id, string Email, string UserType, string? Role)
{
    public bool IsStaff => UserType == "staff";
    public bool IsGuest => UserType == "guest";
}
