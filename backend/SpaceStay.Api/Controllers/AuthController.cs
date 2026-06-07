using Microsoft.AspNetCore.Mvc;
using SpaceStay.Core.Abstractions;
using SpaceStay.Core.Dtos;

namespace SpaceStay.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService auth) : ControllerBase
{
    /// <summary>Autocadastro de hóspede (senha é armazenada com hash).</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterGuestRequest request)
    {
        var result = await auth.RegisterGuestAsync(request);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Login de hóspede ou da equipe; o token reflete o perfil.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        => Ok(await auth.LoginAsync(request));
}
