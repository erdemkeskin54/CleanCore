using CleanCore.Application.Abstractions.Data;
using CleanCore.Application.Abstractions.Services;
using CleanCore.Domain.Shared;
using CleanCore.Domain.Todos;
using CleanCore.Domain.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanCore.Application.Todos.ToggleTodo;

// =============================================================================
// ToggleTodoCommandHandler — todo'yu tamamlandı/tamamlanmadı durumuna çevir
// =============================================================================
// Authorization stratejisi:
//   1) [Authorize] endpoint guard'ı → token doğrulanmış (HTTP layer)
//   2) Handler içinde owner check → todo.UserId != currentUser.UserId → NotOwner (403)
// İkisi farklı katmanda, farklı hata. Token doğru ama yanlış kaynağa erişmeye çalışıyor.
//
// Tracking ON: Toggle() entity'nin state'ini değiştiriyor → EF Core change tracker'ın bu değişikliği
// görmesi gerek. AsNoTracking() kullansak SaveChanges UPDATE atmaz.
// =============================================================================
internal sealed class ToggleTodoCommandHandler : IRequestHandler<ToggleTodoCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public ToggleTodoCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(ToggleTodoCommand request, CancellationToken cancellationToken)
    {
        // Result (non-generic) için implicit cast yok (sadece Result<T>'de var) →
        // explicit Result.Failure(error) ile dönüyoruz.
        if (_currentUser.UserId is not Guid userId)
            return Result.Failure(UserErrors.InvalidCredentials);

        var todo = await _context.Todos
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (todo is null) return Result.Failure(TodoErrors.NotFound);
        if (todo.UserId != userId) return Result.Failure(TodoErrors.NotOwner);

        todo.Toggle();
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
