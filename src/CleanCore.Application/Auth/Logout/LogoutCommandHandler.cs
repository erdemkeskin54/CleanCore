using CleanCore.Application.Abstractions.Authentication;
using CleanCore.Application.Abstractions.Data;
using CleanCore.Domain.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanCore.Application.Auth.Logout;

// =============================================================================
// LogoutCommandHandler — idempotent revoke
// =============================================================================
// Idempotency neden önemli?
//   Logout, retry-safe olmalı. Client double-click attı, network timeout oldu,
//   client retry yaptı → ikinci çağrıda token zaten yok ya da revoke edilmiş.
//   Bu durumda **success** dönmeliyiz: client'ın bakış açısından "logout işlemi
//   tamamlandı" doğru ifade.
//
// Token bulunamadıysa neden 404 değil de success?
//   Logout endpoint'i `[Authorize]` — request zaten valid access token ile geldi.
//   Refresh token DB'de yok → ya zaten revoke edilmiş ya da hiç issue edilmemiş
//   (mobil app yeniden başladı vs). Her iki durumda kullanıcı için sonuç aynı:
//   "logout oldum". UX'te 404 göstermek anlamsız ve gereksiz hata.
// =============================================================================
internal sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly TimeProvider _timeProvider;

    public LogoutCommandHandler(
        IApplicationDbContext context,
        IRefreshTokenGenerator refreshTokenGenerator,
        TimeProvider timeProvider)
    {
        _context = context;
        _refreshTokenGenerator = refreshTokenGenerator;
        _timeProvider = timeProvider;
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = _refreshTokenGenerator.Hash(request.RefreshToken);

        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        // Token yok = zaten "logout" olmuş. Idempotent başarı.
        if (storedToken is null)
            return Result.Success();

        // Idempotent: Revoke iki kez çağrılırsa içeriden no-op (RefreshToken.Revoke kontrol ediyor).
        storedToken.Revoke(_timeProvider.GetUtcNow().UtcDateTime);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
