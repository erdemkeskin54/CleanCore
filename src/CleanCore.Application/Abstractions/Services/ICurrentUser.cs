namespace CleanCore.Application.Abstractions.Services;

// Audit/authorization için "şu an kim istek attı?" bilgisi.
// Faz 2: anonim placeholder implementasyonu (Infrastructure/Services/AnonymousCurrentUser).
// Faz 5: HttpContext + JWT claim'den gerçek değeri dönen implementasyon.
public interface ICurrentUser
{
    Guid? UserId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
}
