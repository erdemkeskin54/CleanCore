namespace CleanCore.Application.Abstractions.Authentication;

// Refresh token üretimi + DB lookup için hash hesaplama.
// İki sorumluluğu tek interface'te tutmamızın sebebi: ikisi de "refresh token
// lifecycle"'ının ayrılmaz parçaları (Generate → client'a ver, Hash → DB'ye yaz).
// Detaylı tasarım notu: Infrastructure/Authentication/RefreshTokenGenerator.cs
public interface IRefreshTokenGenerator
{
    // Ham refresh token string'i üret (cryptographically random). Client'a bu döner.
    // Plain text DB'ye asla yazılmaz — sadece Hash(...) sonucu yazılır.
    string Generate();

    // DB'de saklamak için SHA256 hash'i. Aynı token her zaman aynı hash'i verir
    // (deterministik) — refresh isteklerinde lookup bu hash üzerinden yapılır.
    string Hash(string token);
}
