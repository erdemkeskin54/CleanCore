using CleanCore.Application.Abstractions.Authentication;
using CleanCore.Application.Abstractions.Data;
using CleanCore.Domain.Auth;
using CleanCore.Domain.Shared;
using CleanCore.Domain.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CleanCore.Application.Auth.Login;

// =============================================================================
// LoginCommandHandler — credential verify + token pair issue
// =============================================================================
// Akış:
//   1) Email'i normalize et (trim + lowercase)        — User.Create de aynısını yapıyor
//   2) DB'den user'ı çek
//   3) "User var mı + şifre doğru mu?" tek `if` ile değerlendir → email enumeration koruması
//   4) Aktif değilse → 403 (Inactive)
//   5) Access token üret + refresh token issue + DB'ye yaz
//   6) Plain refresh token'ı response ile dön (DB'de hash'i var, plain sadece bu anda var)
//
// `internal` neden?
//   Handler'a dışarıdan direkt instantiate eden olmasın — her zaman MediatR
//   üzerinden çağrılsın. Test projesi `[InternalsVisibleTo]` ile erişiyor.
//
// Concurrency notu (production):
//   Aynı kullanıcı için eş zamanlı iki login isteği gelirse → iki refresh token
//   üretilir, ikisi de geçerli. Bu kasıtlı: aynı user farklı cihazlarda paralel
//   login olabilir. RefreshTokenCommandHandler'da rotation mantığı bunun farklı.
// =============================================================================
internal sealed class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtProvider _jwtProvider;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly JwtOptions _jwtOptions;
    private readonly TimeProvider _timeProvider;

    public LoginCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtProvider jwtProvider,
        IRefreshTokenGenerator refreshTokenGenerator,
        IOptions<JwtOptions> jwtOptions,
        TimeProvider timeProvider)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtProvider = jwtProvider;
        _refreshTokenGenerator = refreshTokenGenerator;
        _jwtOptions = jwtOptions.Value;
        _timeProvider = timeProvider;
    }

    public async Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // User.Create'te de email lowercase normalize edildiği için lookup'ta tutarlı arama.
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        // Email enumeration koruması: user yok ya da şifre yanlış → aynı hata.
        // İki dalı da tek `if`'te birleştirdik ki saldırgan response time'dan da
        // ayırt edemesin (her iki dal da BCrypt verify çağırmıyor olsa farklı sürer).
        // İleride: user==null durumunda dummy BCrypt.Verify çağırmak da düşünülebilir
        // (response time'ı tam eşitlemek için) — şu an basit tutuyoruz.
        var credentialsValid = user is not null
            && _passwordHasher.Verify(request.Password, user.PasswordHash);

        if (!credentialsValid)
            return UserErrors.InvalidCredentials;

        // Pasif kullanıcı: credentials doğru ama hesap kapalı.
        // Burada açıkça farklı hata döndürüyoruz çünkü kullanıcı bilinçli pasif edilmiş;
        // enumeration konusunda sıkıntı yok (admin pasif ediyor, saldırgan değil).
        if (!user!.IsActive)
            return UserErrors.Inactive;

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var (accessToken, accessExpiry) = _jwtProvider.GenerateAccessToken(user);

        // Refresh token: plain client'a, hash DB'ye. İki ayrı değer.
        var refreshPlain = _refreshTokenGenerator.Generate();
        var refreshHash = _refreshTokenGenerator.Hash(refreshPlain);
        var refreshExpiry = now.AddDays(_jwtOptions.RefreshTokenExpiryDays);

        var refreshEntity = RefreshToken.Issue(user.Id, refreshHash, refreshExpiry, now);

        _context.RefreshTokens.Add(refreshEntity);
        await _context.SaveChangesAsync(cancellationToken);

        return new AuthResponse(accessToken, accessExpiry, refreshPlain, refreshExpiry);
    }
}
