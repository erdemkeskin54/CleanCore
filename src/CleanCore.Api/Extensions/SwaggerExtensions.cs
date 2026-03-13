using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CleanCore.Api.Extensions;

// =============================================================================
// SwaggerExtensions — Swashbuckle + JWT Bearer + per-version doc
// =============================================================================
// Swagger UI'daki "Authorize" butonu nasıl çalışıyor?
//   1) `AddSecurityDefinition("Bearer", ...)` → schema tanımı (JWT Bearer)
//   2) `AddSecurityRequirement(...)` → her endpoint default olarak Bearer ister
//   3) UI'da Authorize butonu → token gir → Swagger her isteğe `Authorization: Bearer {token}` ekler
//
// API versioning entegrasyonu:
//   `ConfigureSwaggerDocPerVersion` her API versiyonu için ayrı Swagger doc üretir.
//   v1, v2, v2.1 olsa: `/swagger/v1/swagger.json`, `/swagger/v2/swagger.json` ...
//   Program.cs'te SwaggerUI provider'dan version listesini alıp dropdown kuruyor.
//
// Niye Swashbuckle 6.8.1, en güncel değil?
//   .NET 10'un default `Microsoft.AspNetCore.OpenApi 10.x` paketi `Microsoft.OpenApi 2.x`'i
//   zorunlu kılıyor. 2.x breaking change var: `OpenApiSecurityScheme.Reference` kaldırılmış.
//   Swashbuckle 10.1.7 henüz 2.x API'sına tam uyumlu değil → JWT security scheme kurarken
//   compile error. 6.8.1 stabil + olgun — JWT auth + per-version docs sorunsuz çalışıyor.
//   Swashbuckle 2.x uyumlu sürümü çıkınca yükseltilecek.
// =============================================================================
public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        // Her API versiyonu için ayrı Swagger doc — `ConfigureSwaggerDocPerVersion` aşağıda.
        services.ConfigureOptions<ConfigureSwaggerDocPerVersion>();

        services.AddSwaggerGen(options =>
        {
            // Bearer auth schema tanımı — Swagger UI'daki "Authorize" butonu için.
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Bearer token. Formatı: `Bearer {token}`",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            // Tüm endpoint'lerde default olarak Bearer auth iste.
            // [AllowAnonymous] olan endpoint'lerde Swagger token'sız da çağırabilir.
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }
}

// =============================================================================
// ConfigureSwaggerDocPerVersion — versiyon başına Swagger doc üretici
// =============================================================================
// Asp.Versioning.ApiExplorer.IApiVersionDescriptionProvider versionları sağlıyor.
// Bu sınıf her birine SwaggerDoc tanımı yazıyor — UI'da `v1`, `v2` dropdown'ları.
//
// Deprecated version'lar:
//   `description.IsDeprecated` flag'i ile UI'da uyarı gösteriliyor — kullanıcılara
//   yeni version'a geçmeyi söylemenin temiz yolu.
// =============================================================================
internal sealed class ConfigureSwaggerDocPerVersion : IConfigureOptions<SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider _provider;

    public ConfigureSwaggerDocPerVersion(IApiVersionDescriptionProvider provider)
    {
        _provider = provider;
    }

    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in _provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, new OpenApiInfo
            {
                Title = "CleanCore API",
                Version = description.ApiVersion.ToString(),
                Description = description.IsDeprecated
                    ? "⚠️ Bu API versiyonu artık desteklenmiyor."
                    : "CleanCore Web API — Clean Architecture template."
            });
        }
    }
}
