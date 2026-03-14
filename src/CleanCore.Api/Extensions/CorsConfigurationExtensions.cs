namespace CleanCore.Api.Extensions;

// =============================================================================
// CorsConfigurationExtensions — config-driven CORS policy
// =============================================================================
// Niye config'ten origin listesi (hardcode değil)?
//   - Dev / staging / prod farklı origin'ler ister (frontend URL'leri farklı)
//   - Devops yeni domain ekleyince kod değişmesin → sadece appsettings update
//   - Liste tek yerde → audit edilmesi kolay
//
// Liste boşsa fallback: AllowAnyOrigin
//   ÖNEMLİ — sadece dev kolaylığı için. Production'da liste boş olmamalı.
//   Boşsa ekrana log uyarısı eklemek (startup'ta) iyi olur — TODO.
//
// `AllowCredentials()` (cookie/auth header forwarding):
//   Liste doluysa cookie/auth header'ları cross-origin geçirebilir. Ama bu
//   özellik AllowAnyOrigin ile birlikte ÇALIŞMAZ (CORS standartı yasaklıyor)
//   — bu yüzden if/else ayrımı var.
//
// Preflight cache:
//   `WithMaxAge` ile preflight (OPTIONS) response'larını cache'lemek browser tarafında
//   throughput'u artırır. Şu an default (varsayılan ~5sn) — ihtiyaç olunca eklenir.
// =============================================================================
public static class CorsConfigurationExtensions
{
    public const string DefaultPolicy = "Default";

    public static IServiceCollection AddCorsPolicy(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // appsettings.json → "Cors": { "AllowedOrigins": ["https://app.example.com", ...] }
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(options =>
        {
            options.AddPolicy(DefaultPolicy, policy =>
            {
                if (origins.Length == 0)
                {
                    // Dev kolaylık modu — production'da liste mutlaka dolu olmalı.
                    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                }
                else
                {
                    // Allow-listed origin'ler + cookie/auth forwarding aktif.
                    policy.WithOrigins(origins)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                }
            });
        });

        return services;
    }
}
