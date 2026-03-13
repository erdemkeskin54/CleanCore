using Asp.Versioning;
using CleanCore.Api.Extensions;
using CleanCore.Application.Users.CreateUser;
using CleanCore.Application.Users.GetUserById;
using Microsoft.AspNetCore.Mvc;

namespace CleanCore.Api.Controllers;

// API versioning eklendi: route şimdi /api/v1/users.
// [Authorize] henüz yok — Faz 5'te eklenecek.
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
[Produces("application/json")]
public sealed class UsersController : ApiControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);

        return result.ToActionResult(id =>
            CreatedAtRoute(nameof(GetById), new { id }, new { id }));
    }

    [HttpGet("{id:guid}", Name = nameof(GetById))]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetUserByIdQuery(id), cancellationToken);

        return result.ToActionResult();
    }
}
