using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpaceStay.Core.Abstractions;
using SpaceStay.Core.Dtos;

namespace SpaceStay.Api.Controllers;

// Ingestão de telemetria chamada pelo simulador IoT (Parte 6). É aberta
// (AllowAnonymous) para facilitar a demo; em produção usaria uma chave de device.
[ApiController]
[Route("api/readings")]
public class ReadingsController(IReadingService readings) : ControllerBase
{
    /// <summary>Recebe uma leitura de sensor; se estiver fora da faixa, gera um alerta.</summary>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ReadingIngestResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReadingIngestResponse>> Ingest([FromBody] CreateReadingRequest request)
    {
        var result = await readings.IngestAsync(request);
        return StatusCode(StatusCodes.Status201Created, result);
    }
}
