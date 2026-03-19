using CleanCore.Domain.Abstractions;

namespace CleanCore.Domain.Auth;

// =============================================================================
// RefreshToken — Rich Domain Model örneği
// =============================================================================
// Güvenlik notları:
//   - DB'de SHA256 **hash**'i saklanır, plain text asla. Leak olsa bile saldırgan
//     hash'ten plain token'a dönemez (SHA256 tek yönlü).
//   - Rotation: her kullanımda `Revoke()` + yeni `Issue()`. Tek kullanımlık — çalınırsa
//     bir sonraki kullanımda hem saldırgan hem gerçek kullanıcı 401 alır, saldırı tespit edilir.
//
// Pattern notları:
//   - **Factory method** (`Issue`): ctor private, instantiation'ı tek bir "anlamlı" noktadan
//     geçiriyoruz. "new RefreshToken(...)" her yerde çağrılmasın, Issue ile niyet belli olsun.
//   - **Rich domain model**: state değişikliği (`Revoke`) + sorgu (`IsActive`) entity'nin içinde.
//     "RefreshTokenService.Revoke(token)" gibi dışarıda yapılsaydı anemic model olurdu — entity
//     sadece property bag'e dönüşürdü, iş kuralları dışarıda dağılırdı.
//   - **now parametresi (DateTime)**: `DateTime.UtcNow` kullanmak entity'i test edilemez yapardı.
//     Caller (handler) `TimeProvider.GetUtcNow()` ile geçiriyor. Test'te FakeTimeProvider ile rotate edilebilir.
// =============================================================================
public class RefreshToken : Entity
{
    // EF Core için parametresiz ctor.
    private RefreshToken() { }

    // Ctor private — dışarıdan direkt `new RefreshToken(...)` yazılamaz.
    // Sadece factory method `Issue` üzerinden yaratılır.
    private RefreshToken(Guid id, Guid userId, string tokenHash, DateTime expiresAt, DateTime createdAt)
        : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        CreatedAt = createdAt;
        IsRevoked = false;
    }

    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    // Factory method — "yeni refresh token vermek" niyetini isim olarak taşır.
    public static RefreshToken Issue(Guid userId, string tokenHash, DateTime expiresAt, DateTime now) =>
        new(Guid.NewGuid(), userId, tokenHash, expiresAt, now);

    // Idempotent: zaten revoke edilmişse sessiz döner. Double-revoke hatası değil.
    public void Revoke(DateTime now)
    {
        if (IsRevoked) return;
        IsRevoked = true;
        RevokedAt = now;
    }

    // "Aktif" = revoke edilmemiş AND süresi dolmamış. Handler'lar tek çağrı ile kontrol eder.
    public bool IsActive(DateTime now) => !IsRevoked && now < ExpiresAt;
}
