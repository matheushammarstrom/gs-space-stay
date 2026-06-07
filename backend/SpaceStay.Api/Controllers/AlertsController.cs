using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpaceStay.Api.Security;
using SpaceStay.Core.Abstractions;
using SpaceStay.Core.Common;
using SpaceStay.Core.Domain;
using SpaceStay.Core.Dtos;

namespace SpaceStay.Api.Controllers;

[ApiController]
[Route("api/alerts")]
[Authorize(Roles = "admin,engineer,concierge")]
public class AlertsController(IAlertService alerts) : ControllerBase
{
    /// <summary>Lista alertas, paginado, com filtro opcional por status (ex.: ?status=open).</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<AlertResponse>>> GetAll(
        [FromQuery] AlertStatus? status, [FromQuery] PageRequest page)
        => Ok(await alerts.GetAlertsAsync(status, page));

    /// <summary>Reconhece um alerta (registra quem tratou em resolved_by).</summary>
    [HttpPut("{id:int}/acknowledge")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AlertResponse>> Acknowledge(int id)
        => Ok(await alerts.AcknowledgeAsync(id, User.RequireCurrentUser()));
}
