using SpaceStay.Core.Abstractions;
using SpaceStay.Core.Common;
using SpaceStay.Core.Domain;
using SpaceStay.Core.Dtos;

namespace SpaceStay.Core.Services;

public class BookingService(
    IBookingRepository bookings,
    IModuleRepository modules) : IBookingService
{
    public async Task<PagedResult<BookingResponse>> GetBookingsAsync(CurrentUser user, PageRequest page)
    {
        // Hóspede vê apenas as próprias reservas; equipe vê todas (RBAC, Parte 5).
        int? guestFilter = user.IsGuest ? user.Id : null;
        var (items, total) = await bookings.GetPagedAsync(guestFilter, page.Skip, page.PageSize);
        var mapped = items.Select(Map).ToList();
        return new PagedResult<BookingResponse>(mapped, page.Page, page.PageSize, total);
    }

    public async Task<BookingResponse> GetByIdAsync(int id, CurrentUser user)
    {
        var b = await bookings.GetByIdWithRefsAsync(id) ?? throw new NotFoundException("Reserva não encontrada.");
        EnsureOwnership(b, user);
        return Map(b);
    }

    public async Task<BookingResponse> CreateAsync(CreateBookingRequest request, CurrentUser user)
    {
        if (!user.IsGuest)
            throw new ForbiddenException("Apenas hóspedes podem criar reservas.");
        if (request.CheckOut <= request.CheckIn)
            throw new DomainValidationException("check_out deve ser posterior a check_in.");

        var module = await modules.GetByIdAsync(request.ModuleId)
            ?? throw new NotFoundException("Módulo não encontrado.");

        if (module.Status == ModuleStatus.maintenance)
            throw new ConflictException("Módulo em manutenção, indisponível para reserva.");

        var occupying = await bookings.CountOccupyingByModuleAsync(module.Id);
        if (occupying >= module.Capacity)
            throw new ConflictException("Módulo sem vagas para o período.");

        var now = DateTime.UtcNow;
        var booking = new Booking
        {
            GuestId = user.Id,
            ModuleId = module.Id,
            CheckIn = request.CheckIn,
            CheckOut = request.CheckOut,
            Status = request.CheckIn <= now ? BookingStatus.active : BookingStatus.confirmed
        };

        await bookings.AddAsync(booking);

        // Se a reserva lota o módulo, marca como ocupado.
        if (occupying + 1 >= module.Capacity)
        {
            module.Status = ModuleStatus.occupied;
            modules.Update(module);
        }

        await bookings.SaveChangesAsync();

        var created = await bookings.GetByIdWithRefsAsync(booking.Id) ?? booking;
        return Map(created);
    }

    public async Task<BookingResponse> UpdateAsync(int id, UpdateBookingRequest request, CurrentUser user)
    {
        var b = await bookings.GetByIdWithRefsAsync(id) ?? throw new NotFoundException("Reserva não encontrada.");
        EnsureOwnership(b, user);

        var newCheckIn = request.CheckIn ?? b.CheckIn;
        var newCheckOut = request.CheckOut ?? b.CheckOut;
        if (newCheckOut <= newCheckIn)
            throw new DomainValidationException("check_out deve ser posterior a check_in.");

        b.CheckIn = newCheckIn;
        b.CheckOut = newCheckOut;
        if (request.Status.HasValue) b.Status = request.Status.Value;

        bookings.Update(b);
        await bookings.SaveChangesAsync();
        return Map(b);
    }

    public async Task DeleteAsync(int id, CurrentUser user)
    {
        var b = await bookings.GetByIdWithRefsAsync(id) ?? throw new NotFoundException("Reserva não encontrada.");
        EnsureOwnership(b, user);
        bookings.Remove(b);
        await bookings.SaveChangesAsync();
    }

    private static void EnsureOwnership(Booking b, CurrentUser user)
    {
        if (user.IsGuest && b.GuestId != user.Id)
            throw new ForbiddenException("Você só pode acessar as suas próprias reservas.");
    }

    private static BookingResponse Map(Booking b) => new(
        b.Id, b.GuestId, b.Guest?.Name, b.ModuleId, b.Module?.Name, b.CheckIn, b.CheckOut, b.Status);
}
