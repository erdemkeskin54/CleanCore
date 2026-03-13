using Asp.Versioning;

namespace CleanCore.Api.Extensions;

// =============================================================================
// ApiVersioningExtensions — URL segment versioning konfigürasyonu
// =============================================================================
// URL'de versiyon: `/api/v1/users`, `/api/v2/users`
//
// Niye URL segment? (Header / Query alternatiflerine karşı)
//   ✓ Tarayıcıda/curl'de açık görünür — debug kolay
//   ✓ Caching proxy'ler (Cloudflare vs) farklı versiyonları ayrı cache'liyor
//   ✓ Route constraint ile compile-time kontrol (`{version:apiVersion}`)
//   ✓ Swagger UI'da doğal görünüyor
//   ✗ Header (`Api-Version: 1.0`): URL'de görünmüyor, postman/curl'de extra adım
//   ✗ Query (`?api-version=1.0`): URL kirliyor, "utility parameter" gibi duruyor
//
// `AssumeDefaultVersionWhenUnspecified = true`:
//   `[ApiVersion("1.0")]` belirtilmediği endpoint'lere default 1.0 atanır.
//   Geçiş döneminde bazı eski endpoint'ler yeni attribute almazsa kırılmasın diye.
//
// `ReportApiVersions = true`:
//   Response header'larına `api-supported-versions` ekler. Client farkındalık için.
//
// `GroupNameFormat = "'v'VVV"`:
//   ApiVersion → grup adı dönüşümü. "1.0" → "v1", "2.1" → "v2.1".
//   SwaggerExtensions'ta bu grup adlarını kullanıyoruz.
//
// `SubstituteApiVersionInUrl = true`:
//   Route template'teki `{version:apiVersion}` placeholder'ını gerçek versiyon
//   ile değiştirir → Swagger doc'lar `/api/v1/users` (placeholder yerine "v1") görür.
// =============================================================================
public static class ApiVersioningExtensions
{
    public static IServiceCollection AddApiVersion(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        })
        .AddMvc()
        .AddApiExplorer(options =>
        {
            // 'v'VVV → "v1", "v1.1" gibi route parametresine girer.
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        return services;
    }
}
