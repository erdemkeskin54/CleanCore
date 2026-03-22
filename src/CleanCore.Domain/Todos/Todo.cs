using CleanCore.Domain.Abstractions;

namespace CleanCore.Domain.Todos;

// =============================================================================
// Todo — örnek aggregate root (User'dan sonra ikinci feature)
// =============================================================================
// Bu entity TEMPLATE'in "yeni feature nasıl eklenir" örneğidir.
// Aynı pattern'i kendi domain'inde uygulayabilirsin (Order, Product, Comment vs.).
//
// Dosya hazırlanma sırası — yeni feature eklerken birebir tekrarla:
//   1) Domain/<Feature>/<Entity>.cs              ← bu dosya (aggregate root)
//   2) Domain/<Feature>/<Entity>Errors.cs        ← hata kodları (UserErrors gibi)
//   3) Infrastructure/Persistence/Configurations/<Entity>Configuration.cs
//   4) Application/Abstractions/Data/IApplicationDbContext.cs → DbSet ekle
//   5) Infrastructure/Persistence/ApplicationDbContext.cs    → DbSet ekle
//   6) `dotnet ef migrations add Add<Feature>` ← schema migration
//   7) Application/<Feature>/<UseCase>/{Command,Validator,Handler}.cs ← her use case için 3 dosya
//   8) Api/Controllers/<Feature>Controller.cs   ← HTTP layer
// Detay: README → "Yeni feature nasıl eklenir" rehberi.
//
// Tasarım notları:
//   - User'a FK ile bağlı (UserId): "kim oluşturdu". Authorization handler'larda yapılıyor
//     (todo.UserId != currentUser.UserId → TodoErrors.NotOwner).
//   - IAuditableEntity → CreatedAt/UpdatedAt + by'ları interceptor otomatik dolduruyor.
//   - ISoftDeletable → context.Todos.Remove(todo) DB'den silmiyor, IsDeleted=true yapıyor
//     (bkz. SoftDeleteInterceptor). Global query filter da silinmiş todo'ları sorguda atlıyor.
//   - Factory + private ctor: instantiation tek noktadan (Todo.Create) — niyet okunaklı.
//   - Domain davranışları (Toggle, UpdateTitle): state değişimi entity'nin içinde, anemic değil.
// =============================================================================
public class Todo : AggregateRoot, IAuditableEntity, ISoftDeletable
{
    // EF Core reflection için parametresiz ctor.
    private Todo() { }

    // Ctor private — dışarıdan `new Todo(...)` yazılamaz, sadece Create factory üzerinden.
    private Todo(Guid id, Guid userId, string title) : base(id)
    {
        UserId = userId;
        Title = title;
        IsCompleted = false;
    }

    public Guid UserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public bool IsCompleted { get; private set; }

    // IAuditableEntity — interceptor (AuditableEntitySaveChangesInterceptor) dolduruyor.
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    // ISoftDeletable — interceptor (SoftDeleteInterceptor) DELETE'i UPDATE IsDeleted=true'ya çeviriyor.
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // Factory — yeni todo oluşturmanın tek meşru yolu.
    // Title trim'leniyor: " süt al " ile "süt al" aynı todo sayılsın.
    public static Todo Create(Guid userId, string title) =>
        new(Guid.NewGuid(), userId, title.Trim());

    // İşlem davranışları — entity'nin state'ini değiştirmenin tek yolu (no public setters).
    public void Toggle() => IsCompleted = !IsCompleted;

    public void UpdateTitle(string newTitle) => Title = newTitle.Trim();
}
