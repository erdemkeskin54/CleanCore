using System.Security.Cryptography;
using System.Text;
using CleanCore.Application.Abstractions.Authentication;

namespace CleanCore.Infrastructure.Authentication;

// =============================================================================
// RefreshTokenGenerator — iki sorumluluk, tek domain
// =============================================================================
// `Generate`: cryptographically-secure random token üretir (client'a gider)
// `Hash`:     aynı tokeni SHA256 ile hash'ler (DB'de saklanır)
//
// Neden tek sınıfta? Token lifecycle'ının iki parçası aslında ayrılmaz:
//   1) Login/Refresh handler'ı Generate() ile token üretir, client'a plain döner.
//   2) Aynı token'ı Hash() ile hash'leyip DB'ye yazar.
//   3) Client refresh'e geldiğinde Hash() ile lookup edilir, plain asla karşılaştırılmaz.
// İki servis yapmak (ITokenGenerator + ITokenHasher) gereksiz abstraction — KISS.
//
// Neden BCrypt değil de SHA256?
//   - Password için BCrypt (yavaş, salted) çünkü kullanıcı password'ü zayıf olabilir —
//     brute force saldırısı gerçek tehdit.
//   - Refresh token için SHA256 yeterli (hızlı, salt'sız) çünkü:
//       * Token zaten 512-bit random entropy'ye sahip — wordlist saldırısı anlamsız
//       * Hash lookup sık (her refresh'te) — BCrypt gibi ~100ms endpoint'i yavaşlatır
//       * DB leak durumunda saldırgan hash'ten plain'e dönemez (SHA256 tek yönlü)
// =============================================================================
internal sealed class RefreshTokenGenerator : IRefreshTokenGenerator
{
    public string Generate()
    {
        // 64 byte = 512 bit entropy. Base64'e çevrilince ~88 karakter string olur.
        // 512 bit brute force için gerekli enerji: gözlemlenebilir evren ömründen uzun.
        // (128 bit zaten yeterliyken 512 seçimi paranoya seviyesi — token boyutu 88 karakter, sorun değil.)
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    public string Hash(string token)
    {
        // SHA256: deterministic → aynı token her zaman aynı hash'i verir → DB lookup çalışır.
        // (BCrypt gibi salt'lı olsaydı her seferinde farklı hash olurdu, lookup imkansızlaşırdı.)
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
