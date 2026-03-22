using CleanCore.Application.Abstractions.Data;
using CleanCore.Application.Abstractions.Services;
using CleanCore.Domain.Shared;
using CleanCore.Domain.Todos;
using CleanCore.Domain.Users;
using MediatR;

namespace CleanCore.Application.Todos.CreateTodo;

// =============================================================================
// CreateTodoCommandHandler — yeni todo kaydı
// =============================================================================
// Akış:
//   1) Current user'ı al (JWT'den geliyor — HttpCurrentUser ICurrentUser implementi)
//   2) UserId yoksa → InvalidCredentials. Buraya gelmek için endpoint [Authorize]
//      olmalı — yine de defansif kontrol.
//   3) Todo.Create factory ile entity oluştur (Title trim'li)
//   4) DbSet'e ekle, SaveChanges
//   5) Yeni id'yi dön → controller CreatedAtRoute kuracak
//
// Yeni handler eklerken bu dosyayı kopyalayıp `<UseCase>CommandHandler.cs` adıyla yapıştır.
// MediatR DI auto-scan: Application.csproj'daki tüm IRequestHandler<,> implementasyonlarını bulur,
// ekstra DI kayıt gerekmiyor (bkz. Application/DependencyInjection.cs `RegisterServicesFromAssembly`).
// =============================================================================
internal sealed class CreateTodoCommandHandler : IRequestHandler<CreateTodoCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;

    public CreateTodoCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(CreateTodoCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is not Guid userId)
            return UserErrors.InvalidCredentials;

        var todo = Todo.Create(userId, request.Title);
        _context.Todos.Add(todo);
        await _context.SaveChangesAsync(cancellationToken);

        return todo.Id;
    }
}
