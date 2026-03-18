using System.Runtime.CompilerServices;
using CleanCore.Application.Abstractions.Authentication;
using CleanCore.Application.Abstractions.Data;
using CleanCore.Application.Abstractions.Services;
using CleanCore.Infrastructure.Authentication;
using CleanCore.Infrastructure.Persistence;
using CleanCore.Infrastructure.Persistence.Interceptors;
using CleanCore.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

// Test projeleri Infrastructure'daki internal sınıfları (BCryptPasswordHasher vb.) test edebilsin.
// Production kodu bu internal'lara erişemez — sadece abstractionlar public.
[assembly: InternalsVisibleTo("CleanCore.UnitTests")]
[assembly: InternalsVisibleTo("CleanCore.IntegrationTests")]

namespace CleanCore.Infrastructure;

// =============================================================================
// AddInfrastructure — Composition Root (Infrastructure katmanı)
// =============================================================================
// Tüm Infrastructure servislerinin DI kaydını tek noktada tutar.
// Program.cs'te `builder.Services.AddInfrastructure(builder.Configuration)` çağrılır.
//
// Lifetime seçimleri:
//   - Singleton: state'siz ve thread-safe servisler (TimeProvider, hasher, JwtProvider).
//   - Scoped:    request ömrü boyunca tekil olması istenenler (DbContext, CurrentUser, interceptor'lar).
//   - Transient: burada yok — kullanırsan her inject'te yeni instance, çoğu zaman istemediğimiz şey.
//
// Options Pattern binding:
//   appsettings.json "Jwt" section → `JwtOptions` tipine bağlanır.
//   Handler/servis `IOptions<JwtOptions>.Value` ile okur.
//   Strongly typed + test'te `Options.Create(new JwtOptions{...})` ile fake geçilebilir.
// =============================================================================
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // TimeProvider.System: production saat. Test'te FakeTimeProvider ile override edilebilir.
        services.AddSingleton(TimeProvider.System);

        // HttpContext erişimi — HttpCurrentUser'ın JWT claim'lerini okuyabilmesi için gerekli.
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();

        // ---- Authentication servisleri
        // "Jwt" section → JwtOptions binding. SectionName sabit `JwtOptions.SectionName`'da.
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        // Singleton — BCryptPasswordHasher, JwtProvider, RefreshTokenGenerator hiçbir state tutmaz.
        // Her request'te yeni instance yaratmanın maliyeti var, ihtiyaç yok.
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IJwtProvider, JwtProvider>();
        services.AddSingleton<IRefreshTokenGenerator, RefreshTokenGenerator>();

        // Interceptor'lar scoped — DbContext ile aynı ömür, ICurrentUser inject edilebilsin diye.
        services.AddScoped<AuditableEntitySaveChangesInterceptor>();
        services.AddScoped<SoftDeleteInterceptor>();

        AddDbContext(services, configuration);

        // IApplicationDbContext ile ApplicationDbContext aynı instance olsun — aynı scope içinde
        // handler'lar IApplicationDbContext inject ederken DI concrete type'a yönlendirir.
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        return services;
    }

    // -------------------------------------------------------------------------
    // DB provider toggle — appsettings: "Database": { "Provider": "Postgres" | "SqlServer" }
    // -------------------------------------------------------------------------
    // Neden runtime toggle (compile-time #if değil)?
    //   - Template'i kullanan kişi rebuild gerektirmeden config'ten geçiş yapabilsin.
    //   - CI'da aynı binary ile farklı DB'ye deploy edilebilsin.
    // Alternatif: provider başına ayrı NuGet paketi + startup kodu. Küçük projede overkill.
    // -------------------------------------------------------------------------
    private static void AddDbContext(IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration.GetValue<string>("Database:Provider") ?? "Postgres";

        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default appsettings'te tanımlı değil.");

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            // Interceptor'lar DI'dan sağlanıyor — böylece ICurrentUser gibi scoped
            // bağımlılıklarını ctor'larından alabilirler.
            options.AddInterceptors(
                sp.GetRequiredService<AuditableEntitySaveChangesInterceptor>(),
                sp.GetRequiredService<SoftDeleteInterceptor>());

            if (string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlServer(connectionString);
            }
            else
            {
                options.UseNpgsql(connectionString);
            }
        });
    }
}
