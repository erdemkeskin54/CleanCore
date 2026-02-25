using CleanCore.Application.Abstractions.Data;
using CleanCore.Infrastructure.Persistence;
using CleanCore.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CleanCore.Infrastructure;

// =============================================================================
// AddInfrastructure — Composition Root (Infrastructure katmanı)
// =============================================================================
// Şu an: DbContext + interceptor'lar + TimeProvider.
// Sonraki commit'lerde: auth servisleri (BCrypt, JwtProvider, RefreshTokenGenerator)
// + HttpCurrentUser eklenecek.
//
// Lifetime seçimleri:
//   - Singleton: state'siz ve thread-safe servisler (TimeProvider).
//   - Scoped:    request ömrü boyunca tekil olması istenenler (DbContext, interceptor'lar).
// =============================================================================
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // TimeProvider.System: production saat. Test'te FakeTimeProvider ile override edilebilir.
        services.AddSingleton(TimeProvider.System);

        // Interceptor'lar scoped — DbContext ile aynı ömür, ICurrentUser inject edilebilsin diye.
        services.AddScoped<AuditableEntitySaveChangesInterceptor>();
        services.AddScoped<SoftDeleteInterceptor>();

        AddDbContext(services, configuration);

        // IApplicationDbContext ile ApplicationDbContext aynı instance olsun.
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        return services;
    }

    private static void AddDbContext(IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration.GetValue<string>("Database:Provider") ?? "Postgres";

        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default appsettings'te tanımlı değil.");

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            // Interceptor'lar DI'dan sağlanıyor — ICurrentUser gibi scoped bağımlılıklarını ctor'larından alabilirler.
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
