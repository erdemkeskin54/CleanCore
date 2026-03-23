using CleanCore.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CleanCore.IntegrationTests;

// =============================================================================
// CleanCoreWebAppFactory — integration test host
// =============================================================================
// WebApplicationFactory<Program> gerçek API'yi in-process çalıştırır. TestServer üzerinden
// HTTP isteği atarız, controller/middleware/handler zinciri tam olarak koşar.
//
// Neden `public partial class Program {}` Program.cs'te? WebApplicationFactory<Program>
// generic parametresinin görünür olması için — top-level program'da partial declaration lazım.
//
// DB override — niye bu kadar tuhaf?
//   AddInfrastructure, DI'ya Npgsql EF Core internal servislerini ekliyor. Test'te InMemory
//   provider'a geçmek istediğimizde naif yaklaşım şu hata fırlatır:
//       "Services for database providers 'Npgsql', 'InMemory' have been registered."
//   EF Core, **tek** bir internal service provider'a izin veriyor — iki provider aynı DI ağacında olamaz.
//
// Çözüm: InMemory için **izole** bir internal service provider inşa ediyoruz ve DbContext'e
// `UseInternalServiceProvider(...)` ile veriyoruz. Böylece InMemory'nin EF Core internals'ı
// kendi ağacında, Npgsql'inkiler shared DI'da — çakışma yok.
//
// Statik + readonly niye? InMemory service provider'ı oluşturmak pahalı (assembly scan vs).
// Tek sefer yapıp tüm test fixture'larının paylaşması CI süresini ciddi azaltıyor.
//
// Parallel-safe mi? EVET.
//   - Her test instance kendi `_dbName`'ini kullanıyor (`test-{Guid}`) → farklı DB instance.
//   - EF Core InMemory database name bazlı izolasyon yapıyor → testler birbirini etkilemez.
//   - Static provider sadece EF Core'un **internal** servislerini sağlıyor (thread-safe).
// =============================================================================
public sealed class CleanCoreWebAppFactory : WebApplicationFactory<Program>
{
    private static readonly IServiceProvider InMemoryServiceProvider = new ServiceCollection()
        .AddEntityFrameworkInMemoryDatabase()
        .BuildServiceProvider();

    // Her factory instance (→ her test sınıfı) kendi DB adını alır. Testler birbirinin
    // verisini görmez — paralel koşma güvenli.
    private readonly string _dbName = $"test-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // ConfigureTestServices: app'in kendi ConfigureServices'i koştuktan SONRA çalışır.
        // Böylece AddInfrastructure'ın eklediği Npgsql DbContext'i `RemoveAll` ile kaldırıp
        // yerine InMemory koyabiliyoruz.
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<DbContextOptions>();

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName);
                options.UseInternalServiceProvider(InMemoryServiceProvider);
            });
        });
    }
}
