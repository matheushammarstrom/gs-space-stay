using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpaceStay.Core.Abstractions;
using SpaceStay.Core.Common;
using SpaceStay.Core.Dtos;

namespace SpaceStay.Api.Controllers;

[ApiController]
[Route("api/modules")]
[Authorize]
public class ModulesController(IModuleService modules) : ControllerBase
{
    /// <summary>Lista os módulos com status e quantidade de alertas em aberto (paginado).</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<ModuleResponse>>> GetAll([FromQuery] PageRequest page)
        => Ok(await modules.GetModulesAsync(page));

    /// <summary>Detalhe de um módulo.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ModuleResponse>> Get(int id)
        => Ok(await modules.GetModuleAsync(id));

    /// <summary>Configuração dos sensores do módulo (com faixas seguras).</summary>
    [HttpGet("{id:int}/sensors")]
    public async Task<ActionResult<List<SensorResponse>>> Sensors(int id)
        => Ok(await modules.GetSensorsAsync(id));

    /// <summary>Última leitura de cada sensor do módulo (telemetria atual).</summary>
    [HttpGet("{id:int}/readings")]
    public async Task<ActionResult<List<ReadingResponse>>> Readings(int id)
        => Ok(await modules.GetLatestReadingsAsync(id));
}
