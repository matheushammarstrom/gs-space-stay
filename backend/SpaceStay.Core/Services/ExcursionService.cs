using SpaceStay.Core.Abstractions;
using SpaceStay.Core.Common;
using SpaceStay.Core.Domain;
using SpaceStay.Core.Dtos;

namespace SpaceStay.Core.Services;

public class ExcursionService(
    IExcursionRepository excursions,
    IExcursionBookingRepository excursionBookings) : IExcursionService
{
    public async Task<List<ExcursionResponse>> GetExcursionsAsync(CurrentUser user)
    {
        // Quais excursões o hóspede atual já reservou (para marcar BookedByMe).
        var myIds = user.IsGuest
            ? (await excursionBookings.GetByGuestAsync(user.Id))
                .Where(eb => eb.Status != ExcursionBookingStatus.cancelled)
                .Select(eb => eb.ExcursionId).ToHashSet()
            : new HashSet<int>();

        var all = await excursions.ListAllAsync();
        var result = new List<ExcursionResponse>();
        foreach (var e in all.OrderBy(e => e.ScheduledAt))
        {
            var booked = await excursions.CountActiveBookingsAsync(e.Id);
            result.Add(new ExcursionResponse(e.Id, e.Name, e.Description, e.Capacity, e.ScheduledAt,
                booked, Math.Max(0, e.Capacity - booked), myIds.Contains(e.Id)));
        }
        return result;
    }

    public async Task<BookExcursionResponse> BookAsync(int excursionId, CurrentUser user)
    {
        if (!user.IsGuest)
            throw new ForbiddenException("Apenas hóspedes podem reservar excursões.");

        var excursion = await excursions.GetByIdAsync(excursionId)
            ?? throw new NotFoundException("Excursão não encontrada.");

        if (await excursionBookings.ExistsAsync(user.Id, excursionId))
            throw new ConflictException("Você já reservou esta excursão.");

        var booked = await excursions.CountActiveBookingsAsync(excursionId);
        if (booked >= excursion.Capacity)
            throw new ConflictException("Excursão lotada.");

        var eb = new ExcursionBooking
        {
            GuestId = user.Id,
            ExcursionId = excursionId,
            Status = ExcursionBookingStatus.booked
        };
        await excursionBookings.AddAsync(eb);
        await excursionBookings.SaveChangesAsync();

        return new BookExcursionResponse(eb.Id, excursionId, user.Id, eb.Status);
    }

    public async Task<ExcursionResponse> CancelAsync(int excursionId, CurrentUser user)
    {
        if (!user.IsGuest)
            throw new ForbiddenException("Apenas hóspedes podem cancelar reservas.");

        var excursion = await excursions.GetByIdAsync(excursionId)
            ?? throw new NotFoundException("Excursão não encontrada.");

        var booking = await excursionBookings.GetAsync(user.Id, excursionId)
            ?? throw new NotFoundException("Você não tem reserva nesta excursão.");

        // Remove de fato a reserva para liberar a vaga e permitir reservar de novo.
        excursionBookings.Remove(booking);
        await excursionBookings.SaveChangesAsync();

        var booked = await excursions.CountActiveBookingsAsync(excursionId);
        return new ExcursionResponse(excursion.Id, excursion.Name, excursion.Description, excursion.Capacity,
            excursion.ScheduledAt, booked, Math.Max(0, excursion.Capacity - booked), BookedByMe: false);
    }
}
