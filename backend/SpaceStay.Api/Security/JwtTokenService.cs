using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SpaceStay.Core.Abstractions;

namespace SpaceStay.Api.Security;

// Emite tokens JWT assinados (HMAC-SHA256) com as claims do usuário.
public class JwtTokenService(IConfiguration config) : ITokenService
{
    public TokenResult GenerateToken(int userId, string email, string userType, string? role)
    {
        var jwt = config.GetSection("Jwt");
        var key = jwt["Key"] ?? throw new InvalidOperationException("Jwt:Key não configurado.");
        var minutes = int.TryParse(jwt["ExpiresMinutes"], out var m) ? m : 120;
        var expires = DateTime.UtcNow.AddMinutes(minutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new("user_type", userType),
            new(ClaimTypes.Role, role ?? userType),   // hóspedes recebem role "guest"
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        return new TokenResult(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
