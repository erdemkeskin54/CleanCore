using CleanCore.Domain.Shared;
using MediatR;

namespace CleanCore.Application.Todos.CreateTodo;

// Yeni todo oluşturma command'i. Owner otomatik gelir (ICurrentUser).
// Result<Guid>: oluşan todo'nun id'si — controller 201 + Location header için kullanır.
public sealed record CreateTodoCommand(string Title) : IRequest<Result<Guid>>;
