using SpaceStay.Core.Common;
using SpaceStay.Core.Domain;
using SpaceStay.Core.Dtos;
using SpaceStay.Core.Services;
using SpaceStay.Infra.Security;
using SpaceStay.Tests.Unit.Fakes;
using Xunit;

namespace SpaceStay.Tests.Unit;

// Testes do AuthService (cadastro e login com hash): casos TC1 e TC2.
public class AuthServiceTests
{
    private readonly FakeGuestRepository _guests = new();
    private readonly FakeStaffRepository _staff = new();
    private readonly Pbkdf2PasswordHasher _hasher = new();
    private AuthService CreateSut() => new(_guests, _staff, _hasher, new FakeTokenService());

    [Fact] // TC1: cadastro armazena a senha com hash (não em texto puro)
    public async Task RegisterGuest_armazena_senha_hasheada_e_retorna_token()
    {
        var sut = CreateSut();
        var resp = await sut.RegisterGuestAsync(new RegisterGuestRequest
        {
            Name = "Ana", Email = "ana@example.com", Password = "Senha@123", MedicalClearance = true
        });

        Assert.Equal("guest", resp.UserType);
        Assert.False(string.IsNullOrWhiteSpace(resp.Token));

        var saved = Assert.Single(_guests.Items);
        Assert.NotEqual("Senha@123", saved.PasswordHash);            // não é texto puro
        Assert.True(_hasher.Verify(saved.PasswordHash, "Senha@123")); // mas confere
    }

    [Fact]
    public async Task RegisterGuest_com_email_existente_lanca_conflito()
    {
        _guests.Items.Add(new Guest { Id = 1, Name = "Ana", Email = "ana@example.com", PasswordHash = "x" });
        var sut = CreateSut();

        await Assert.ThrowsAsync<ConflictException>(() => sut.RegisterGuestAsync(new RegisterGuestRequest
        {
            Name = "Outra Ana", Email = "ana@example.com", Password = "Senha@123"
        }));
    }

    [Fact] // TC2: login com senha errada não autentica
    public async Task Login_com_senha_errada_lanca_AuthenticationException()
    {
        _guests.Items.Add(new Guest
        {
            Id = 1, Name = "Ana", Email = "ana@example.com", PasswordHash = _hasher.Hash("Senha@123")
        });
        var sut = CreateSut();

        await Assert.ThrowsAsync<AuthenticationException>(() => sut.LoginAsync(new LoginRequest
        {
            Email = "ana@example.com", Password = "errada"
        }));
    }

    [Fact]
    public async Task Login_correto_de_guest_retorna_token()
    {
        _guests.Items.Add(new Guest
        {
            Id = 7, Name = "Ana", Email = "ana@example.com", PasswordHash = _hasher.Hash("Senha@123")
        });
        var sut = CreateSut();

        var resp = await sut.LoginAsync(new LoginRequest { Email = "ana@example.com", Password = "Senha@123" });
        Assert.Equal("guest", resp.UserType);
        Assert.Equal(7, resp.Id);
    }

    [Fact]
    public async Task Login_correto_de_staff_retorna_role()
    {
        _staff.Items.Add(new Staff
        {
            Id = 2, Name = "Rafael", Email = "rafael@spacestay.space",
            Role = StaffRole.engineer, PasswordHash = _hasher.Hash("Engineer@123")
        });
        var sut = CreateSut();

        var resp = await sut.LoginAsync(new LoginRequest { Email = "rafael@spacestay.space", Password = "Engineer@123" });
        Assert.Equal("staff", resp.UserType);
        Assert.Equal("engineer", resp.Role);
    }

    [Fact]
    public async Task Login_com_email_inexistente_lanca_AuthenticationException()
    {
        var sut = CreateSut();
        await Assert.ThrowsAsync<AuthenticationException>(() => sut.LoginAsync(new LoginRequest
        {
            Email = "ninguem@example.com", Password = "x"
        }));
    }
}
