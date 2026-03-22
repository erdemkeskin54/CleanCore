using CleanCore.Application.Abstractions.Data;
using CleanCore.Application.Abstractions.Services;
using CleanCore.Domain.Shared;
using CleanCore.Domain.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanCore.Application.Todos.GetMyTodos;

// =============================================================================
// GetMyTodosQueryHandler — current user'ın todo'larını listele
// =============================================================================
// Read path optimizasyonları:
//   - AsNoTracking(): change tracker kapalı (read-only sorgu)
//   - .Where(t => t.UserId == userId): authorization scope — başkasının todo'larını görmesin
//   - Soft-delete query filter otomatik (ApplicationDbContext.ApplySoftDeleteQueryFilter)
//   - .Select projection: sadece DTO'ya gerekli kolonlar — Title gibi 200-char alanları gereksizce çekmiyoruz
//   - .OrderByDescending(CreatedAt): en yeni üstte
//
// İleride pagination eklenirse:
//   - Skip/Take parametrelerini Query'e ekle (Page, PageSize)
//   - Toplam count'u ayrı sorguyla dön (PagedResult<T> gibi)
// =============================================================================
internal sealed class GetMyTodosQueryHandler
    : IRequestHandler<GetMyTodosQuery, Result<IReadOnlyList<TodoDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetMyTodosQueryHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<TodoDto>>> Handle(
        GetMyTodosQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not Guid userId)
            return Result.Failure<IReadOnlyList<TodoDto>>(UserErrors.InvalidCredentials);

        var todos = await _context.Todos
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TodoDto(t.Id, t.Title, t.IsCompleted, t.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<TodoDto>>(todos);
    }
}
