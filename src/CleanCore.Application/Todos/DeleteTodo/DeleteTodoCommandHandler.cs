using CleanCore.Application.Abstractions.Data;
using CleanCore.Application.Abstractions.Services;
using CleanCore.Domain.Shared;
using CleanCore.Domain.Todos;
using CleanCore.Domain.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanCore.Application.Todos.DeleteTodo;

// =============================================================================
// DeleteTodoCommandHandler — soft delete
// =============================================================================
// `_context.Todos.Remove(todo)` çağrısını fiziksel DELETE sanma — SoftDeleteInterceptor
// (Infrastructure/Persistence/Interceptors/) bu işlemi yakalar ve UPDATE IsDeleted=true'ya
// çevirir + DeletedAt/DeletedBy alanlarını doldurur.
//
// Sonuçta DB'ye giden SQL DELETE değil UPDATE — kayıt silinmemiş, "görünmez" olmuş.
// Sonraki SELECT sorgularında global query filter (`!IsDeleted`) bunu gizler.
//
// Hard delete istersen: `context.Database.ExecuteSqlAsync("DELETE FROM todos WHERE id = ...")`.
// Şu an için soft delete tek yol — kasıtlı, yanlışlıkla fiziksel silme yapmayalım.
// =============================================================================
internal sealed class DeleteTodoCommandHandler : IRequestHandler<DeleteTodoCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public DeleteTodoCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeleteTodoCommand request, CancellationToken cancellationToken)
    {
        // Result (non-generic) için implicit cast yok → explicit Result.Failure(error).
        if (_currentUser.UserId is not Guid userId)
            return Result.Failure(UserErrors.InvalidCredentials);

        var todo = await _context.Todos
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (todo is null) return Result.Failure(TodoErrors.NotFound);
        if (todo.UserId != userId) return Result.Failure(TodoErrors.NotOwner);

        // Remove → SoftDeleteInterceptor UPDATE IsDeleted=true'ya çeviriyor.
        _context.Todos.Remove(todo);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
