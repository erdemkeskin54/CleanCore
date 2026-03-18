using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CleanCore.Application.Abstractions.Authentication;
using CleanCore.Domain.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CleanCore.Infrastructure.Authentication;

// =============================================================================
// JwtProvider — access token üretici
// =============================================================================
// İçinde ne var?
//   - sub   = User.Id (subject)         — claim-based auth için kullanıcı kimliği
//   - email = User.Email                — client UI'ın gösterebilmesi için
//   - jti   = Guid.NewGuid()            — tokenin tekilliği (revocation list'lerinde tracking için)
//   - name  = User.FullName             — User.Identity.Name üzerinden okunur
//
// Algoritma: HmacSha256
//   - Symmetric: aynı anahtar hem imzalar hem doğrular. Tek servis için ideal.
//   - Alternatif: RS256 (asymmetric) — public key ayrı servislere dağıtılır.
//     Microservice mimarisinde RS256 tercih edilir; monolitte HS256 yeterli.
//
// Anahtar boyutu: HmacSha256 min 256 bit (32 byte) ister.
//   appsettings'teki SigningKey >= 32 ASCII karakter olmalı; aksi halde runtime'da
//   "key size must be greater than..." exception alırız.
//
// Imza doğrulama nerede? `src/CleanCore.Api/Extensions/AuthenticationExtensions.cs`
// `ValidateIssuer`, `ValidateAudience`, `ValidateLifetime`, `ValidateIssuerSigningKey` hepsi true.
// =============================================================================
internal sealed class JwtProvider : IJwtProvider
{
    private readonly JwtOptions _options;
    private readonly TimeProvider _timeProvider;

    public JwtProvider(IOptions<JwtOptions> options, TimeProvider timeProvider)
    {
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    public (string Token, DateTime ExpiresAt) GenerateAccessToken(User user)
    {
        // TimeProvider inject: test'te FakeTimeProvider ile "saat ileri-geri" senaryosu oynanabilir.
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var expires = now.AddMinutes(_options.AccessTokenExpiryMinutes);

        // JwtRegisteredClaimNames: JWT standard claim name'leri ("sub", "email"...).
        // Standart string sabitlerini kullanmak magic string hatalarını önler.
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Name, user.FullName)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(jwt);
        return (tokenString, expires);
    }
}
