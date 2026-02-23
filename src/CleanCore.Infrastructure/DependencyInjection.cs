using CleanCore.Application.Abstractions.Data;
using CleanCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CleanCore.Infrastructure;

// =============================================================================
// AddInfrastructure — Composition Root (Infrastructure katmanı)
// =============================================================================
// Şu an: DbContext + Postgres/SqlServer toggle.
// Sonraki commit'lerde: interceptor'lar + auth servisleri eklenecek.
// =============================================================================
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        AddDbContext(services, configuration);

        // IApplicationDbContext ile ApplicationDbContext aynı instance olsun.
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        return services;
    }

    // Provider toggle — appsettings'ten "Database:Provider" okunuyor (Postgres/SqlServer).
    // Runtime toggle, compile-time #if değil — template kullanıcısı rebuild gerektirmeden config'ten geçiş yapabilsin.
    private static void AddDbContext(IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration.GetValue<string>("Database:Provider") ?? "Postgres";

        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default appsettings'te tanımlı değil.");

        services.AddDbContext<ApplicationDbContext>(options =>
        {
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
