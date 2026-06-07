using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpaceStay.Api.Security;
using SpaceStay.Core.Abstractions;
using SpaceStay.Core.Dtos;

namespace SpaceStay.Api.Controllers;

[ApiController]
[Route("api/excursions")]
[Authorize]
public class ExcursionsController(IExcursionService excursions) : ControllerBase
{
    /// <summary>Lista as excursões com vagas disponíveis.</summary>
    [HttpGet]
    public async Task<ActionResult<List<ExcursionResponse>>> GetAll()
        => Ok(await excursions.GetExcursionsAsync(User.RequireCurrentUser()));

    /// <summary>Hóspede reserva uma excursão (valida capacidade e duplicidade).</summary>
    [HttpPost("{id:int}/book")]
    [Authorize(Roles = "guest")]
    [ProducesResponseType(typeof(BookExcursionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookExcursionResponse>> Book(int id)
    {
        var result = await excursions.BookAsync(id, User.RequireCurrentUser());
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Hóspede cancela a própria reserva da excursão (libera a vaga).</summary>
    [HttpDelete("{id:int}/book")]
    [Authorize(Roles = "guest")]
    [ProducesResponseType(typeof(ExcursionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExcursionResponse>> Cancel(int id)
        => Ok(await excursions.CancelAsync(id, User.RequireCurrentUser()));
}
