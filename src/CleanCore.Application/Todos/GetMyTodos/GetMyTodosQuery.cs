using CleanCore.Domain.Shared;
using MediatR;

namespace CleanCore.Application.Todos.GetMyTodos;

// Parametresiz query — current user'ın todo'larını döner.
// `IRequest<Result<IReadOnlyList<TodoDto>>>` — caller liste alıyor (TanStack Query bunu cache'liyor).
public sealed record GetMyTodosQuery() : IRequest<Result<IReadOnlyList<TodoDto>>>;
