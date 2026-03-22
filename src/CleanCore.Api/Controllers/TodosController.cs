using Asp.Versioning;
using CleanCore.Api.Extensions;
using CleanCore.Application.Todos.CreateTodo;
using CleanCore.Application.Todos.DeleteTodo;
using CleanCore.Application.Todos.GetMyTodos;
using CleanCore.Application.Todos.ToggleTodo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanCore.Api.Controllers;

// =============================================================================
// TodosController — örnek CRUD endpoint
// =============================================================================
// Tüm endpoint'ler `[Authorize]` (controller seviyesinde) — token zorunlu.
// Authorization scope'u (sadece kendi todo'larına erişim) HANDLER'larda yapılıyor
// (todo.UserId != currentUser.UserId → 403).
//
// Endpoint listesi:
//   GET    /api/v1/todos             → kendi todo'larım
//   POST   /api/v1/todos             → yeni todo
//   PUT    /api/v1/todos/{id}/toggle → tamamlandı/değil çevir
//   DELETE /api/v1/todos/{id}        → soft delete
//
// Yeni controller eklerken bu dosyayı kopyalayıp <Feature>Controller.cs adıyla yapıştır.
// HTTP verb seçimi:
//   - GET     → idempotent okuma (liste/detay)
//   - POST    → yeni kayıt (server id üretiyor)
//   - PUT     → idempotent güncelleme (tüm kaynak)
//   - PATCH   → kısmi güncelleme (sadece değişen alanlar)
//   - DELETE  → silme (idempotent)
// Toggle PUT — RESTful: state'i bilinen yeni değere set etmiyoruz, "tersine çevir" ama tek action.
// PATCH da olabilirdi; PUT seçimi pratiklik tercihi.
// =============================================================================
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/todos")]
[Authorize]
[Produces("application/json")]
public sealed class TodosController : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TodoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMy(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetMyTodosQuery(), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create(
        CreateTodoCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);

        // CreatedAtAction → 201 + Location header (yeni kaynak nereye?)
        // Liste endpoint'ine yönlendiriyoruz; tek tek detay endpoint'i şimdilik yok.
        return result.ToActionResult(id =>
            CreatedAtAction(nameof(GetMy), new { }, new { id }));
    }

    [HttpPut("{id:guid}/toggle")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Toggle(Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new ToggleTodoCommand(id), cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new DeleteTodoCommand(id), cancellationToken);
        return result.ToActionResult();
    }
}
