using Asp.Versioning.ApiExplorer;
using CleanCore.Api.Extensions;
using CleanCore.Api.Middleware;
using CleanCore.Application;
using CleanCore.Application.Abstractions.Authentication;
using CleanCore.Infrastructure;
using CleanCore.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Serilog;

// =============================================================================
// Program.cs — composition root + middleware pipeline
// =============================================================================
// Bu dosya iki sorumluluğu birleştirir:
//   1) Service registration (DI container'a kim hangi servisi sağlıyor)
//   2) HTTP middleware pipeline ordering (request hangi sırayla işlenecek)
//
// Pipeline sırası KRİTİK — yanlış yerleştirme = sessiz bug.
// Sırayı değiştirmeden önce iki kez düşün, test'leri koştur.
// =============================================================================

var builder = WebApplication.CreateBuilder(args);

// -----------------------------------------------------------------------------
// 1) Logging — Serilog host-level
// -----------------------------------------------------------------------------
// `UseSerilog` Microsoft.Extensions.Logging'i replace eder.
// `ReadFrom.Configuration` → appsettings'teki Serilog section'ından okur (sink, level vs).
// `Enrich.FromLogContext` → CorrelationIdMiddleware'in push'ladığı property'leri
// her log satırına otomatik ekler.
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration)
                 .Enrich.FromLogContext());

// -----------------------------------------------------------------------------
// 2) Katman registration — Application + Infrastructure composition root'ları
// -----------------------------------------------------------------------------
// Her katman kendi DependencyInjection'unda kayıt yapıyor — burası sadece çağırıyor.
// Bağımlılık yönü: Api → Infrastructure → Application → Domain (Clean Architecture).
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// -----------------------------------------------------------------------------
// 3) API altyapısı — controllers + cross-cutting concerns
// -----------------------------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddCorsPolicy(builder.Configuration);
builder.Services.AddApiVersion();
builder.Services.AddSwaggerWithJwt();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddApiHealthChecks(builder.Configuration);

// IExceptionHandler + ProblemDetails — beklenmedik exception'ları RFC 7807 response'a çeviriyor.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// -----------------------------------------------------------------------------
// Dev kolaylığı: migration'ları startup'ta otomatik uygula
// -----------------------------------------------------------------------------
// `dotnet ef database update` manuel çağrısı gerekmez — `dotnet run` tek başına yeterli.
// Production'da KAPALI: schema değişikliği deliberate olmalı (deployment pipeline'ında ayrı step).
// IsRelational() guard'ı: integration test'lerinde InMemory provider migration desteklemediği için skip.
// -----------------------------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (db.Database.IsRelational())
    {
        await db.Database.MigrateAsync();

        // Demo kullanıcı seed'i — `dotnet run` sonrası login için ne kullanacağını
        // hatırlamaya gerek kalmasın. Idempotent: zaten varsa no-op.
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        await DatabaseSeeder.SeedAsync(db, hasher);
    }
}

// =============================================================================
// HTTP Pipeline (sıra önemli — yorumdaki numaralar pipeline akışını gösteriyor)
// =============================================================================
//
//   [in]  Request gelir
//     │
//     ▼
//   1) Serilog request logging       → her request method+path+duration log
//   2) CorrelationIdMiddleware       → X-Correlation-Id ekle, LogContext'e push
//   3) UseExceptionHandler           → handler'lara giden exception'ları yakala
//   4) Swagger UI (dev only)         → /swagger endpoint'i
//   5) HttpsRedirection              → HTTP → HTTPS redirect (prod)
//   6) Cors                          → preflight + origin kontrolü
//   7) Authentication                → JWT'yi doğrula, ClaimsPrincipal kur
//   8) Authorization                 → [Authorize] attribute'lerini kontrol et
//   9) MapControllers                → controller routing
//  10) Health checks                 → /health, /health/ready
//     │
//     ▼
//   [out] Response gider
//
// =============================================================================

app.UseSerilogRequestLogging();
app.UseMiddleware<CorrelationIdMiddleware>();

// ExceptionHandler ÇOK ERKENDE — alttaki middleware'lerden gelen tüm exception'ları yakalar.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        // Versioning ile dropdown'da v1, v2... göstermek için her version için endpoint kayıt.
        var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
        foreach (var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerEndpoint(
                $"/swagger/{description.GroupName}/swagger.json",
                description.GroupName.ToUpperInvariant());
        }
    });

    // Root URL'i Swagger'a yönlendir — `dotnet run` sonrası "/" 'a giren ekipteki herkes
    // 404 görmek yerine doğrudan API katalogunu görsün.
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}

// HttpsRedirection sadece production'da. Dev'de launchSettings HTTP-only profil seçildiğinde
// "Failed to determine the https port for redirect" warning'i çıkıyor — gereksiz gürültü.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors(CorsConfigurationExtensions.DefaultPolicy);

// !!! AUTHENTICATION MUTLAKA AUTHORIZATION'DAN ÖNCE !!!
// Aksi halde Authorization "kimliği bilmediği user'ı authorize edemez" → her endpoint 401.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Liveness probe — sadece "process ayakta mı?" — DB down olsa bile 200.
app.MapHealthChecks("/health");

// Readiness probe — DB dahil tüm "ready" tag'lı check'ler. Orchestrator buradan trafik kararı veriyor.
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();

// `WebApplicationFactory<Program>` integration test'lerin Program tipine erişebilmesi için.
// `partial` çünkü top-level statement'larda compiler implicit partial sınıf yaratıyor.
public partial class Program { }
