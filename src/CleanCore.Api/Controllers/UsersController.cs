using Asp.Versioning;
using CleanCore.Api.Extensions;
using CleanCore.Application.Users.CreateUser;
using CleanCore.Application.Users.GetUserById;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanCore.Api.Controllers;

// =============================================================================
// UsersController — örnek CRUD endpoint'i (Create + GetById)
// =============================================================================
// İleride genişletilecekler:
//   - PUT /api/v1/users/{id}        → UpdateUserCommand
//   - DELETE /api/v1/users/{id}     → DeleteUserCommand (soft delete interceptor halleder)
//   - GET /api/v1/users (paged)     → ListUsersQuery
//   - PATCH .../password            → ChangePasswordCommand
//
// `[ApiVersion("1.0")]` + URL segment versioning ile bu controller `v1` altında.
// v2 geldiğinde aynı controller `[ApiVersion("1.0", "2.0")]` ile çoklu version'lı
// yapılır ya da ayrı `UsersV2Controller` açılır.
// =============================================================================
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
[Produces("application/json")]
public sealed class UsersController : ApiControllerBase
{
    // Kayıt endpoint'i — public.
    // İleride admin-only "create user" + ayrı public "register" akışı (email confirm)
    // ayırılırsa burası `[Authorize(Roles = "Admin")]` olur.
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);

        // CreatedAtRoute → 201 + Location header (yeni user'a pointer).
        // Body'de id'yi de dönüyoruz çünkü REST best practice + UI tarafı routing'e ihtiyaç duyabilir.
        return result.ToActionResult(id =>
            CreatedAtRoute(nameof(GetById), new { id }, new { id }));
    }

    [HttpGet("{id:guid}", Name = nameof(GetById))]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetUserByIdQuery(id), cancellationToken);

        return result.ToActionResult();
    }
}
