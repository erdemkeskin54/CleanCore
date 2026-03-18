using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CleanCore.Application.Abstractions.Services;
using Microsoft.AspNetCore.Http;

namespace CleanCore.Infrastructure.Services;

// =============================================================================
// HttpCurrentUser — JWT claim'lerinden ICurrentUser implementasyonu
// =============================================================================
// Hangi katmanda ne biliyor?
//   - Application: `ICurrentUser` interface'i — UserId, Email, IsAuthenticated
//   - Infrastructure (burası): HttpContext + JWT claim mantığı (HTTP-aware)
//
// `IHttpContextAccessor` neden tercih edildi?
//   `IHttpContextAccessor` thread-safe ve scoped — her request'te aynı HttpContext'e
//   erişir. Singleton servisin içinden de güvenle kullanılabilir.
//
// Claim okuma stratejisi:
//   1) JWT standard ad ("sub", "email") — `JwtSecurityTokenHandler.DefaultMapInboundClaims = false`
//      olduğu için bu adlar ham gelir
//   2) Fallback: ClaimTypes.NameIdentifier / ClaimTypes.Email — eğer ileride başka
//      auth provider eklenirse (Cookie auth, OAuth) onun da koyduğu claim'leri yakalar
//
// Background job senaryosu:
//   Hangfire/Hosted service'ten DbContext kullanılırsa HttpContext yok → User null →
//   audit interceptor "system" yazar. ICurrentUser'ı override eden bir SystemCurrentUser
//   ekleyip job scope'unda kullanmak mümkün — şu an yok, gerekirse kolay.
// =============================================================================
internal sealed class HttpCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public HttpCurrentUser(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    private ClaimsPrincipal? User => _accessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            // Önce JWT standard "sub", sonra fallback NameIdentifier (cookie auth vs için).
            var sub = User?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public string? Email =>
        User?.FindFirstValue(JwtRegisteredClaimNames.Email)
        ?? User?.FindFirstValue(ClaimTypes.Email);

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
}
