using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using SpaceStay.Core.Abstractions;
using SpaceStay.Core.Common;

namespace SpaceStay.Api.Security;

// Monta o CurrentUser a partir das claims do JWT já validado.
public static class CurrentUserExtensions
{
    public static CurrentUser? GetCurrentUser(this ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true) return null;

        var idStr = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                    ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(idStr, out var id)) return null;

        var email = principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value
                    ?? principal.FindFirst(ClaimTypes.Email)?.Value ?? "";
        var userType = principal.FindFirst("user_type")?.Value ?? "guest";
        var role = principal.FindFirst(ClaimTypes.Role)?.Value;

        return new CurrentUser(id, email, userType, role);
    }

    public static CurrentUser RequireCurrentUser(this ClaimsPrincipal principal)
        => principal.GetCurrentUser() ?? throw new AuthenticationException("Token inválido ou ausente.");
}
