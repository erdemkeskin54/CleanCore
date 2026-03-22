namespace CleanCore.Application.Todos.GetMyTodos;

// Read model — UI'ya gidecek alanlar.
// UserId DTO'ya konmuyor: zaten "current user'ın listesi" geliyor, gereksiz bilgi.
// Audit + soft-delete bayrakları da yok — UI bunlarla ilgilenmez.
public sealed record TodoDto(
    Guid Id,
    string Title,
    bool IsCompleted,
    DateTime CreatedAt);
