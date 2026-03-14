namespace CleanCore.Api.Extensions;

// =============================================================================
// HealthCheckExtensions — DB provider'a göre uygun health check
// =============================================================================
// Endpoint'ler (Program.cs'te map ediliyor):
//   /health        → tüm health check'ler (info amaçlı)
//   /health/ready  → sadece "ready" tag'lı (DB dahil) — orchestrator readiness probe
//
// Niye iki ayrı endpoint?
//   - Kubernetes / Docker Swarm liveness vs readiness ayrımı yapar:
//       liveness → "process ayakta mı?"          → /health
//       readiness → "trafik almaya hazır mı?"   → /health/ready
//   - readiness DB'ye bağlanamıyor olabilir (geçici); orchestrator trafiği başka pod'a yönlendirir
//   - liveness DB up/down umurunda değil — sadece API hayatta mı diye bakar (restart kararı)
//
// DB provider toggle:
//   Aynı `Database:Provider` config flag'i (Infrastructure'daki) burada da kullanılıyor —
//   Postgres için NpgSql, SqlServer için SqlServer health check paketi. İkisi de
//   "aynı tag'lerle ('ready', 'db')" register oluyor → aynı endpoint davranışı.
//
// İleride:
//   - Redis health check (cache layer eklenirse)
//   - SMTP health check (email service)
//   - External API health check (Stripe vs)
//   Hepsi aynı `healthChecks.Add...` zinciriyle eklenir.
// =============================================================================
public static class HealthCheckExtensions
{
    public static IServiceCollection AddApiHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var provider = configuration.GetValue<string>("Database:Provider") ?? "Postgres";
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default tanımlı değil.");

        var healthChecks = services.AddHealthChecks();

        if (string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            healthChecks.AddSqlServer(connectionString, name: "sqlserver", tags: ["ready", "db"]);
        }
        else
        {
            healthChecks.AddNpgSql(connectionString, name: "postgres", tags: ["ready", "db"]);
        }

        return services;
    }
}
