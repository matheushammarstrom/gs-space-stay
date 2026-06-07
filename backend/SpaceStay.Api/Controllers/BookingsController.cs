using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpaceStay.Api.Security;
using SpaceStay.Core.Abstractions;
using SpaceStay.Core.Common;
using SpaceStay.Core.Dtos;

namespace SpaceStay.Api.Controllers;

[ApiController]
[Route("api/bookings")]
[Authorize]
public class BookingsController(IBookingService bookings) : ControllerBase
{
    /// <summary>Lista reservas, paginado (equipe vê todas; hóspede só as suas).</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<BookingResponse>>> GetAll([FromQuery] PageRequest page)
        => Ok(await bookings.GetBookingsAsync(User.RequireCurrentUser(), page));

    /// <summary>Detalhe de uma reserva.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BookingResponse>> GetById(int id)
        => Ok(await bookings.GetByIdAsync(id, User.RequireCurrentUser()));

    /// <summary>Cria uma reserva (valida disponibilidade e capacidade do módulo).</summary>
    [HttpPost]
    [Authorize(Roles = "guest")]
    [ProducesResponseType(typeof(BookingResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookingResponse>> Create([FromBody] CreateBookingRequest request)
    {
        var created = await bookings.CreateAsync(request, User.RequireCurrentUser());
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Altera/cancela uma reserva.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingResponse>> Update(int id, [FromBody] UpdateBookingRequest request)
        => Ok(await bookings.UpdateAsync(id, request, User.RequireCurrentUser()));

    /// <summary>Remove uma reserva.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        await bookings.DeleteAsync(id, User.RequireCurrentUser());
        return NoContent();
    }
}
