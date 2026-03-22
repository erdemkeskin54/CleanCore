using CleanCore.Domain.Shared;
using MediatR;

namespace CleanCore.Application.Todos.DeleteTodo;

// Soft delete — todo'yu fiziksel olarak silmiyor, IsDeleted=true yapıyor.
// SoftDeleteInterceptor halletyor (context.Todos.Remove(todo) → UPDATE IsDeleted=true).
public sealed record DeleteTodoCommand(Guid Id) : IRequest<Result>;
