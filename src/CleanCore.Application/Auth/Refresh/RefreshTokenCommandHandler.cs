using CleanCore.Application.Abstractions.Authentication;
using CleanCore.Application.Abstractions.Data;
using CleanCore.Domain.Auth;
using CleanCore.Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CleanCore.Application.Auth.Refresh;

// =============================================================================
// RefreshTokenCommandHandler — Refresh Token Rotation
// =============================================================================
// Saldırı modeli:
//   Saldırgan refresh token'ı çaldı, kullanıcı haberi yok.
//   Klasik refresh: token 7 gün geçerli, saldırgan 7 gün boyunca yenileyebilir.
//   Rotation ile: saldırgan token'ı bir kere kullanır, gerçek user da bir kere
//   kullanır → ikinci kullanım 401 → kullanıcı relogin'e zorlanır → SALDIRI TESPİT EDİLİR.
//
// Akış:
//   1) Gelen plain token'ı hash'le → DB lookup
//   2) Token yok ya da revoke/expire ise → 401
//   3) Token sahibi user pasif ise → 401
//   4) Eski token'ı revoke et (`storedToken.Revoke(now)`)
//   5) Yeni access + refresh pair üret + DB'ye yaz
//   6) Yeni pair'ı response ile dön
//
// Concurrency notu (production):
//   "Saldırgan ve user aynı anda token'ı kullanırsa" senaryosunda race var.
//   Şu an basit `FirstOrDefaultAsync` + `Revoke` + `SaveChanges` zinciri.
//   Production'da `SELECT ... FOR UPDATE` (Postgres) veya pessimistic lock ile
//   atomik yapmak iyi olur. Şimdilik single-use'un detection edici tarafı yeterli —
//   ikisi de revoke edilir, bir sonraki refresh'te ikisi de invalid olur.
//
// Concurrency tradeoff'u kabul edilebilir mi?
//   En kötü senaryo: race window'da iki yeni token üretilir, eskisi revoke edilir.
//   Sonraki refresh'te hangisi gelse de invalid olur → güvenlik sızıntısı yok.
//   UX zararı: kullanıcı bir kere extra relogin yapar — kabul edilebilir.
// =============================================================================
internal sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtProvider _jwtProvider;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly JwtOptions _jwtOptions;
    private readonly TimeProvider _timeProvider;

    public RefreshTokenCommandHandler(
        IApplicationDbContext context,
        IJwtProvider jwtProvider,
        IRefreshTokenGenerator refreshTokenGenerator,
        IOptions<JwtOptions> jwtOptions,
        TimeProvider timeProvider)
    {
        _context = context;
        _jwtProvider = jwtProvider;
        _refreshTokenGenerator = refreshTokenGenerator;
        _jwtOptions = jwtOptions.Value;
        _timeProvider = timeProvider;
    }

    public async Task<Result<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        // Plain → hash ile DB lookup. Plain token DB'de yok, sadece hash var.
        var tokenHash = _refreshTokenGenerator.Hash(request.RefreshToken);

        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        // Token yok | revoke edilmiş | süresi dolmuş — hepsi aynı hata mesajı.
        // Saldırgan "token revoke mu yoksa hiç yok mu" ayrımını öğrenmemeli.
        if (storedToken is null || !storedToken.IsActive(now))
            return AuthErrors.InvalidRefreshToken;

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == storedToken.UserId, cancellationToken);

        // User silinmiş ya da pasif edilmiş — aynı 401.
        if (user is null || !user.IsActive)
            return AuthErrors.InvalidRefreshToken;

        // ROTATION: Eski token'ı **mutlaka** revoke et (single-use guarantee).
        // Aynı transaction içinde Add + Revoke → SaveChanges atomik.
        storedToken.Revoke(now);

        var (accessToken, accessExpiry) = _jwtProvider.GenerateAccessToken(user);

        var newRefreshPlain = _refreshTokenGenerator.Generate();
        var newRefreshHash = _refreshTokenGenerator.Hash(newRefreshPlain);
        var newRefreshExpiry = now.AddDays(_jwtOptions.RefreshTokenExpiryDays);
        var newRefreshEntity = RefreshToken.Issue(user.Id, newRefreshHash, newRefreshExpiry, now);

        _context.RefreshTokens.Add(newRefreshEntity);
        await _context.SaveChangesAsync(cancellationToken);

        return new AuthResponse(accessToken, accessExpiry, newRefreshPlain, newRefreshExpiry);
    }
}
