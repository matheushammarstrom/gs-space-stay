using SpaceStay.Core.Abstractions;
using SpaceStay.Core.Common;
using SpaceStay.Core.Domain;
using SpaceStay.Core.Dtos;

namespace SpaceStay.Core.Services;

public class AuthService(
    IGuestRepository guests,
    IStaffRepository staff,
    IPasswordHasher hasher,
    ITokenService tokens) : IAuthService
{
    public async Task<AuthResponse> RegisterGuestAsync(RegisterGuestRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        // Não pode haver colisão de e-mail nem com hóspede nem com a equipe.
        if (await guests.EmailExistsAsync(email) || await staff.GetByEmailAsync(email) is not null)
            throw new ConflictException("Já existe um usuário cadastrado com esse e-mail.");

        var guest = new Guest
        {
            Name = request.Name.Trim(),
            Email = email,
            PasswordHash = hasher.Hash(request.Password),   // senha sempre com hash (Parte 5)
            Nationality = string.IsNullOrWhiteSpace(request.Nationality) ? null : request.Nationality!.Trim(),
            MedicalClearance = request.MedicalClearance,
            CreatedAt = DateTime.UtcNow
        };

        await guests.AddAsync(guest);
        await guests.SaveChangesAsync();

        var token = tokens.GenerateToken(guest.Id, guest.Email, "guest", null);
        return new AuthResponse(token.Token, token.ExpiresAt, "guest", null, guest.Id, guest.Name);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        // 1) tenta como hóspede
        var guest = await guests.GetByEmailAsync(email);
        if (guest is not null && hasher.Verify(guest.PasswordHash, request.Password))
        {
            var t = tokens.GenerateToken(guest.Id, guest.Email, "guest", null);
            return new AuthResponse(t.Token, t.ExpiresAt, "guest", null, guest.Id, guest.Name);
        }

        // 2) tenta como equipe
        var member = await staff.GetByEmailAsync(email);
        if (member is not null && hasher.Verify(member.PasswordHash, request.Password))
        {
            var role = member.Role.ToString();
            var t = tokens.GenerateToken(member.Id, member.Email, "staff", role);
            return new AuthResponse(t.Token, t.ExpiresAt, "staff", role, member.Id, member.Name);
        }

        // Mensagem única: não revela se o e-mail existe (boa prática da Parte 5).
        throw new AuthenticationException("E-mail ou senha inválidos.");
    }
}
