namespace CleanCore.Application.Abstractions.Authentication;

// =============================================================================
// JwtOptions — .NET "Options Pattern"
// =============================================================================
// Appsettings'teki "Jwt" section'ından binding ile doldurulur. Nerede?
//   src/CleanCore.Infrastructure/DependencyInjection.cs →
//     services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
//
// Handler/servis içinde nasıl erişilir?
//   ctor(IOptions<JwtOptions> opts) → opts.Value.Issuer
//
// Neden direkt IConfiguration değil?
//   - Strongly typed (Issuer property'si vs config["Jwt:Issuer"] magic string)
//   - Validation hook'ları eklenebilir (ValidateDataAnnotations)
//   - Test'te `Options.Create(new JwtOptions { ... })` ile fake geçilebilir
//   - Section değişirse tek yer: SectionName sabiti
//
// SigningKey için güvenlik notu:
//   HmacSha256 kullanıyoruz → anahtar minimum 256 bit (32 ASCII karakter) olmalı.
//   appsettings'teki default sadece dev için; prod'da secret manager'dan gelmeli
//   (Azure Key Vault, AWS Secrets Manager, dotnet user-secrets vs).
// =============================================================================
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SigningKey { get; set; } = string.Empty;

    // 15 dakika: access token kısa ömürlü olsun, çalınırsa pencere dar.
    // Kullanıcı rahatsız olmasın diye 7 gün refresh token ile uzatılıyor.
    public int AccessTokenExpiryMinutes { get; set; } = 15;

    // 7 gün: yaygın default. Daha kısa (ör. 1 gün) = daha sık login; daha uzun = çalıntı pencere büyür.
    public int RefreshTokenExpiryDays { get; set; } = 7;
}
