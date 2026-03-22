using CleanCore.Domain.Shared;
using MediatR;

namespace CleanCore.Application.Todos.ToggleTodo;

// Tamamlandı durumunu çevir (true ↔ false). Owner-only.
// Result (non-generic) — başarılı toggle'da response body olmaz, controller 204 döner.
public sealed record ToggleTodoCommand(Guid Id) : IRequest<Result>;
