namespace CleanCore.Application.Abstractions.Authentication;

// =============================================================================
// AuthResponse — Login & Refresh dönüş tipi
// =============================================================================
// Token pair'ı (access + refresh) ve son kullanma tarihlerini birlikte taşır.
//
// Neden expiry'leri de dönüyoruz?
//   Client tarafı (mobil/web SPA) "ne zaman refresh atayım?" kararını verirken
//   token'ı parse edip exp claim'ine bakmak zorunda kalmasın diye. Sunucu zaten
//   biliyor; iki taraf aynı sayıyı paylaşsın.
//
// Neden record?
//   - Immutable: response oluşturulduktan sonra değişmesin.
//   - Value equality bedava — test asserter'larında kolaylık.
//   - JSON serialization'da clean output.
//
// İleride: ID token (OpenID Connect) eklenirse buraya `IdToken` alanı katılır.
//          Scope-based auth eklenirse `Scope` da gelir.
// =============================================================================
public sealed record AuthResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt);
