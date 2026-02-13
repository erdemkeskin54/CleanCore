using CleanCore.Domain.Abstractions;

namespace CleanCore.Domain.Users;

// İlk örnek aggregate root. Faz 3'te use case'ler yazılırken üzerine davranış eklenecek.
// Property'ler private setter — state sadece domain metotlarıyla değişsin.
public class User : AggregateRoot, IAuditableEntity, ISoftDeletable
{
    // EF Core reflection ile bu ctor'ı çağırır.
    private User() { }

    private User(Guid id, string email, string passwordHash, string fullName)
        : base(id)
    {
        Email = email;
        PasswordHash = passwordHash;
        FullName = fullName;
        IsActive = true;
    }

    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    // IAuditableEntity — interceptor dolduruyor.
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    // ISoftDeletable — interceptor delete'i convert ediyor.
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    public static User Create(string email, string passwordHash, string fullName) =>
        new(Guid.NewGuid(), email.Trim().ToLowerInvariant(), passwordHash, fullName.Trim());

    public void UpdateProfile(string fullName) => FullName = fullName.Trim();

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    public void ChangePasswordHash(string newHash) => PasswordHash = newHash;
}
