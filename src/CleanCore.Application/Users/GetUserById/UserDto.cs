namespace CleanCore.Application.Users.GetUserById;

// Read model — kullanıcı detay endpoint'inin response shape'i.
// PasswordHash, audit alanları, soft delete bayrakları DTO'ya **katılmaz** —
// API tüketicisinin görmesi gerekenler buraya girer.
//
// Record kullanımı:
//   - Immutable, value equality bedava
//   - Ctor + property tanımı tek satır
//   - JSON serialization'da clean output
public sealed record UserDto(
    Guid Id,
    string Email,
    string FullName,
    bool IsActive,
    DateTime CreatedAt);
