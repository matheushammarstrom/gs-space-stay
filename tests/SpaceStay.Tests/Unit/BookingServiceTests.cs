using SpaceStay.Core.Abstractions;
using SpaceStay.Core.Common;
using SpaceStay.Core.Domain;
using SpaceStay.Core.Dtos;
using SpaceStay.Core.Services;
using SpaceStay.Tests.Unit.Fakes;
using Xunit;

namespace SpaceStay.Tests.Unit;

// Testes do BookingService (capacidade, disponibilidade e RBAC): casos TC3 e TC4.
public class BookingServiceTests
{
    private readonly FakeBookingRepository _bookings = new();
    private readonly FakeModuleRepository _modules = new();
    private BookingService CreateSut() => new(_bookings, _modules);

    private static readonly CurrentUser Guest = new(1, "g@example.com", "guest", "guest");
    private static readonly CurrentUser Staff = new(9, "s@spacestay.space", "staff", "engineer");

    private Module AddModule(int id, int capacity, ModuleStatus status = ModuleStatus.available)
    {
        var m = new Module { Id = id, Name = $"Mod {id}", Type = ModuleType.suite, Capacity = capacity, Status = status };
        _modules.Items.Add(m);
        return m;
    }

    [Fact] // TC3: reserva em módulo com vaga
    public async Task Create_em_modulo_disponivel_cria_reserva()
    {
        AddModule(1, capacity: 2);
        var sut = CreateSut();

        var resp = await sut.CreateAsync(new CreateBookingRequest
        {
            ModuleId = 1, CheckIn = DateTime.UtcNow.AddDays(-1), CheckOut = DateTime.UtcNow.AddDays(2)
        }, Guest);

        Assert.Equal(1, resp.ModuleId);
        Assert.Equal(Guest.Id, resp.GuestId);
        Assert.Single(_bookings.Items);
    }

    [Fact] // TC4: reserva em módulo lotado retorna 409
    public async Task Create_em_modulo_lotado_lanca_conflito()
    {
        AddModule(1, capacity: 2);
        _bookings.Items.Add(new Booking { Id = 100, ModuleId = 1, GuestId = 2, Status = BookingStatus.active });
        _bookings.Items.Add(new Booking { Id = 101, ModuleId = 1, GuestId = 3, Status = BookingStatus.active });
        var sut = CreateSut();

        await Assert.ThrowsAsync<ConflictException>(() => sut.CreateAsync(new CreateBookingRequest
        {
            ModuleId = 1, CheckIn = DateTime.UtcNow, CheckOut = DateTime.UtcNow.AddDays(1)
        }, Guest));
    }

    [Fact]
    public async Task Create_em_modulo_em_manutencao_lanca_conflito()
    {
        AddModule(1, capacity: 2, status: ModuleStatus.maintenance);
        var sut = CreateSut();

        await Assert.ThrowsAsync<ConflictException>(() => sut.CreateAsync(new CreateBookingRequest
        {
            ModuleId = 1, CheckIn = DateTime.UtcNow, CheckOut = DateTime.UtcNow.AddDays(1)
        }, Guest));
    }

    [Fact]
    public async Task Create_com_datas_invalidas_lanca_validacao()
    {
        AddModule(1, capacity: 2);
        var sut = CreateSut();

        await Assert.ThrowsAsync<DomainValidationException>(() => sut.CreateAsync(new CreateBookingRequest
        {
            ModuleId = 1, CheckIn = DateTime.UtcNow.AddDays(2), CheckOut = DateTime.UtcNow.AddDays(1)
        }, Guest));
    }

    [Fact]
    public async Task Create_por_membro_da_equipe_e_proibido()
    {
        AddModule(1, capacity: 2);
        var sut = CreateSut();

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.CreateAsync(new CreateBookingRequest
        {
            ModuleId = 1, CheckIn = DateTime.UtcNow, CheckOut = DateTime.UtcNow.AddDays(1)
        }, Staff));
    }

    [Fact] // TC7: hóspede não acessa reserva de outro hóspede
    public async Task GetById_de_outro_hospede_e_proibido()
    {
        _bookings.Items.Add(new Booking { Id = 5, ModuleId = 1, GuestId = 999, Status = BookingStatus.active });
        var sut = CreateSut();

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.GetByIdAsync(5, Guest));
    }

    [Fact]
    public async Task Create_em_modulo_inexistente_lanca_notfound()
    {
        var sut = CreateSut();
        await Assert.ThrowsAsync<NotFoundException>(() => sut.CreateAsync(new CreateBookingRequest
        {
            ModuleId = 404, CheckIn = DateTime.UtcNow, CheckOut = DateTime.UtcNow.AddDays(1)
        }, Guest));
    }
}
