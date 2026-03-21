using Asp.Versioning;
using CleanCore.Api.Extensions;
using CleanCore.Application.Auth.Login;
using CleanCore.Application.Auth.Logout;
using CleanCore.Application.Auth.Refresh;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanCore.Api.Controllers;

// =============================================================================
// AuthController — login / refresh / logout endpoint'leri
// =============================================================================
// Endpoint stratejisi:
//   - login: `[AllowAnonymous]` (default — controller'da hiçbir auth attribute yok)
//   - refresh: aynı (kullanıcı access token'ı süresi dolduğunda hala anonim)
//   - logout: `[Authorize]` — geçerli access token'la birlikte refresh'i revoke eder
//
// Niye logout `[Authorize]`?
//   Logout'a authorize gerekmese, herhangi biri başkasının refresh token string'ini
//   yakalarsa onun adına logout edebilir. Authorize → kullanıcının kim olduğunu
//   doğrular, kendi token'ını revoke ettiğini garantiler.
//
// Controller ince:
//   `Mediator.Send(command)` + `result.ToActionResult()` — iki satır.
//   Tüm iş mantığı handler'da, HTTP layer sadece dispatch + mapping.
// =============================================================================
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
[Produces("application/json")]
public sealed class AuthController : ApiControllerBase
{
    [HttpPost("login")]
    [ProducesResponseType(typeof(Application.Abstractions.Authentication.AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Login(LoginCommand command, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(Application.Abstractions.Authentication.AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(LogoutCommand command, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return result.ToActionResult();
    }
}
