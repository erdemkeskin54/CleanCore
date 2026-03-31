# CleanCore — İlerleme Takibi

Her faz tek oturumda (1-2 saat) bitebilir. Bir faz bitmeden sonrakine geçme.
Seans bittiğinde aşağıdaki **Session notları** bölümüne 1-2 satır not düş.

---

## Faz 1 — Foundation (Temel) — ✅ Tamamlandı (2026-04-23)

**Amaç:** Solution, projeler, referanslar, Domain primitif'leri (Entity, ValueObject, Result).

- [x] `CleanCore.slnx` solution oluşturuldu (.NET 10 yeni XML format)
- [x] 4 src projesi oluşturuldu (Domain, Application, Infrastructure, Api)
- [x] 2 test projesi oluşturuldu (UnitTests, IntegrationTests)
- [x] Proje referansları kuruldu (Clean Architecture bağımlılık yönü)
- [x] `.gitignore`, `.editorconfig`, `global.json`, `Directory.Build.props` yazıldı
- [x] Domain katmanında `Entity`, `AggregateRoot`, `ValueObject` base class'ları yazıldı
- [x] `Result` + `Result<T>` + `Error` pattern'i yazıldı
- [x] `IAuditableEntity`, `ISoftDeletable` yazıldı (domain event dosyaları sonradan kaldırıldı — revizyon notuna bak)
- [x] `dotnet build` — 0 warning, 0 error
- [x] 8 unit test yeşil (Result pattern + ValueObject eşitlik davranışı)

**Bitiş notu:** Faz 1 solid. Solution build temiz, test'ler yeşil. .NET 10 default'ta `.slnx` yeni format
kullanıyor — VS 2022 ve Rider destekliyor. `docs/ARCHITECTURE.md` bu kararın detayını tutuyor.

---

## Faz 2 — Persistence (Kalıcılık) — ✅ Tamamlandı (2026-04-23)

**Amaç:** EF Core + PostgreSQL + migration + IApplicationDbContext + soft delete + audit.

- [x] EF Core 10 + Npgsql + SqlServer provider + Design paketleri eklendi
- [x] `ApplicationDbContext` yazıldı (Infrastructure/Persistence/)
- [x] Soft delete otomatik global query filter (ISoftDeletable impl eden her entity'ye expression tree ile)
- [x] Audit columns otomatik doldurma (`AuditableEntitySaveChangesInterceptor`)
- [x] Soft delete interceptor (DELETE → UPDATE IsDeleted=true otomatik çeviri)
- [x] `IApplicationDbContext` abstraction (Application katmanında — Jason Taylor style; generic repository değil, detay `ARCHITECTURE.md`'de)
- [x] İlk migration oluşturuldu (`InitialCreate` — users tablosu, unique email index)
- [x] Connection string + `Database:Provider` config (Postgres default, SqlServer toggle'ı)
- [x] `docker-compose.yml` ile Postgres 16 Alpine + healthcheck
- [x] Domain'e örnek `User` aggregate + `UserErrors` + `UserRegisteredEvent` eklendi
- [x] `ICurrentUser` abstraction + Faz 5'te değişecek `AnonymousCurrentUser` placeholder
- [x] `TimeProvider` (built-in) audit zaman damgaları için
- [x] `.config/dotnet-tools.json` ile lokal `dotnet-ef` tool manifest

**Bitiş notu:** Migration oluşturuldu, build temiz, 8 test yeşil. Postgres'i çalıştırıp `dotnet ef database update` ile şemayı kurabilirsin.

---

## Faz 3 — Application (Uygulama Katmanı) — ✅ Tamamlandı (2026-04-23)

**Amaç:** MediatR + FluentValidation + pipeline behaviors + örnek kullanıcı use case'leri.

- [x] MediatR 12.4.1 (Apache 2.0, son ücretsiz major) + assembly scan ile handler registrasyonu
- [x] FluentValidation 12 + assembly scan ile validator registrasyonu
- [x] `ValidationBehavior<,>` — validator'lar varsa otomatik koşuyor, hata → ValidationException
- [x] `LoggingBehavior<,>` — her request için "Handling X", "Handled X in Yms" log'u
- [x] `UnhandledExceptionBehavior<,>` — beklenmedik exception'ları log'lar, ValidationException'a karışmaz
- [x] `CreateUserCommand` + `CreateUserCommandValidator` + `CreateUserCommandHandler` — 3 dosya, vertical slice
- [x] `GetUserByIdQuery` + `GetUserByIdQueryHandler` + `UserDto` — manuel `Select(...)` projection, AutoMapper yok
- [x] `Program.cs` → `AddApplication()` çağrısı eklendi
- [x] `[assembly: InternalsVisibleTo("CleanCore.UnitTests")]` — internal handler'lar test edilebilir
- [x] 10 yeni unit test (4 validator + 2 CreateUser handler + 2 GetUserById handler, + önceki 8)
- [x] Toplam 18 test yeşil, build 0 warning 0 error

**Bitiş notu:** Faz 3 solid. MediatR pipeline tam çalışıyor, handler'lar Result pattern dönüyor, validator'lar otomatik koşuyor. Handler testleri EF Core InMemory provider ile. Faz 4'te HTTP layer + global exception middleware ile her şey son kullanıcıya açılacak.

---

## Faz 4 — API Layer — ✅ Tamamlandı (2026-04-23)

**Amaç:** Controller'lar, middleware, Swagger, ProblemDetails, CORS, versioning, health checks, Serilog.

- [x] `ApiControllerBase` — lazy `ISender` property, controller'lar ince kalıyor
- [x] `GlobalExceptionHandler` (.NET 8+ `IExceptionHandler`) → ProblemDetails + ValidationException field-bazlı errors
- [x] `CorrelationIdMiddleware` → `X-Correlation-Id` header + Serilog LogContext
- [x] Swagger UI + JWT Bearer button (Authorize butonu hazır — Faz 5'te gerçek token'la kullanılacak)
- [x] API versioning URL segment (`/api/v1/users`) + Asp.Versioning + Swagger entegrasyonu
- [x] CORS policy config-driven (`Cors:AllowedOrigins` listesi, boşsa dev için AllowAnyOrigin)
- [x] `UsersController` — `POST /api/v1/users`, `GET /api/v1/users/{id}` (Create + GetById)
- [x] Health checks — `/health` (tüm) + `/health/ready` (sadece "ready" tag'li, DB dahil)
- [x] `ResultExtensions.ToActionResult()` → Result → IActionResult mapping (404/400/409/401/403/500)
- [x] Serilog yapılandırıldı (Serilog.AspNetCore), console'a structured log
- [x] WeatherForecast default dosyaları temizlendi
- [x] `docker-compose.yml` dev için hazır (Faz 2'de oluşturulmuştu, artık API de tam ayağa kalkıyor)
- [x] Integration test'ler (WebApplicationFactory + EF InMemory) — 3 test yeşil
- [x] Toplam **21 test yeşil** (18 unit + 3 integration), build 0 warning 0 error

**Paket kararları:**
- `Swashbuckle.AspNetCore 6.8.1` — .NET 10'un `Microsoft.AspNetCore.OpenApi 10.0.5`'i `Microsoft.OpenApi 2.x`'i zorluyordu ama Swashbuckle 10.1.7 ile çakışıyor. 6.8.1 (OpenApi 1.x ile uyumlu, olgun, stabil) ile pin'ledik.
- `Asp.Versioning.Mvc 10.0.0` + `Asp.Versioning.Mvc.ApiExplorer 10.0.0`
- `Serilog.AspNetCore 10.0.0`
- `AspNetCore.HealthChecks.NpgSql / SqlServer 9.0.0`

**Bitiş notu:** HTTP layer tam çalışıyor. Postgres ayaktayken `docker compose up -d` + `dotnet ef database update` + `dotnet run` → Swagger UI'dan Create/Get user çalışır. Faz 5'te JWT + refresh token + BCrypt ile gerçek auth ekleyeceğiz.

---

## Faz 5 — Security (Güvenlik) — ✅ Tamamlandı (2026-04-23)

**Amaç:** JWT + Refresh Token Rotation + Password Hashing + Authorization.

- [x] JWT oluşturma (HmacSha256) — `JwtProvider`, `Microsoft.AspNetCore.Authentication.JwtBearer` ile doğrulama
- [x] Refresh token **rotation**: her refresh eski tokeni revoke eder, yeni pair döner. Rotation attack-proof.
- [x] Refresh token DB'de SHA256 hash olarak saklanır (plain text hiçbir yerde disk'te yok)
- [x] Password hashing — `BCryptPasswordHasher` (work factor 11, ~100ms)
- [x] `[Authorize]` endpoint'i: `GET /api/v1/users/{id}`
- [x] `AuthController` — `login`, `refresh`, `logout` (logout `[Authorize]`)
- [x] `HttpCurrentUser` — JWT claim'lerinden UserId + Email okuyan gerçek `ICurrentUser` implementasyonu
- [x] `AnonymousCurrentUser` placeholder (Faz 2) silindi
- [x] `JwtSecurityTokenHandler.DefaultMapInboundClaims = false` — claim adları olduğu gibi ("sub", "email") kullanılıyor
- [x] `Domain/Auth/RefreshToken` entity + `refresh_tokens` tablosu + unique index on TokenHash
- [x] Migration `AddRefreshTokens` oluşturuldu
- [x] `CreateUserCommandHandler` password'ü BCrypt ile hash'liyor
- [x] `[Authorize]` öncesi `UseAuthentication()` pipeline'a eklendi
- [x] **Role-based authorization** → ertelendi (Faz 6 / roadmap'te, örnek policy yeterli değil, ayrı tasarım lazım)
- [x] Unit test: `BCryptPasswordHasherTests` (4 test) + güncel `CreateUserCommandHandlerTests` (fake hasher)
- [x] Integration test: `AuthControllerTests` (login 401 + full rotation flow — eski token yeniden kullanımı 401 dönüyor doğrulaması) + güncel `UsersControllerTests` (auth-required GET flow)
- [x] **27 test yeşil** (22 unit + 5 integration), build 0 warning 0 error

**Paket kararları:**
- `Microsoft.AspNetCore.Authentication.JwtBearer 10.0.7`
- `BCrypt.Net-Next 4.1.0`
- Infrastructure'a `<FrameworkReference Include="Microsoft.AspNetCore.App" />` eklendi (IHttpContextAccessor için)

**Bitiş notu:** Auth tam çalışıyor. Role-based auth Faz 6'ya ertelendi — generic role middleware yerine kullanım senaryosuna özgü (ör. admin/user) politika tasarlamak lazım. Detay `docs/ROADMAP.md`.

---

## Faz 6 — Template + CI — ✅ Tamamlandı (2026-04-23)

**Amaç:** `dotnet new cleancore -n MyProject` template'i + GitHub Actions + Docker + NuGet pack.

- [x] `.template.config/template.json` yazıldı — shortName `cleancore`, `sourceName: "CleanCore"` (namespace otomatik rename), `--DbProvider` (Postgres/SqlServer) + `--JwtSigningKey` + `--skipRestore` parametreleri
- [x] `CleanCore.Template.csproj` — `PackageType=Template`, content-only pack, `docs/` + `.github/` + kendi csproj'u exclude
- [x] `dotnet new install .` ile lokal test yapıldı — `dotnet new cleancore -n DemoApi` → namespace `DemoApi`'ye çevrildi, DB adı lowercase'a da düştü, `Jwt.Issuer/Audience` de yenilendi
- [x] Üretilen `DemoApi` projesi 0 warning/0 error build oldu, **27 test yeşil** (22 unit + 5 integration)
- [x] `--DbProvider SqlServer` doğrulandı — `appsettings.json` `"Provider": "SqlServer"`
- [x] `Dockerfile` (multi-stage, Alpine 10.0-alpine, non-root `app` user, 8080 exposed) + `.dockerignore`
- [x] `.github/workflows/ci.yml` — build + test (push/PR main), ayrı `pack` job main push'ta nupkg üretip artifact'a yüklüyor
- [x] `dotnet pack CleanCore.Template.csproj` → `artifacts/CleanCore.Template.1.0.0.nupkg` (~68 KB) üretildi, içerik doğrulandı (`docs/` ve `.github/` hariç, `.template.config/` dahil, tüm migration + test dosyaları dahil)
- [x] NuGet paketi lokal kuruldu (`dotnet new install ./artifacts/CleanCore.Template.1.0.0.nupkg`), ondan üretilen proje build oldu → paket yayına hazır
- [x] `README.md` — template install/use talimatları + parametre tablosu + Docker çalıştırma + end-to-end auth flow

**Paket kararları:**
- `PackageType=Template` + `IncludeBuildOutput=false` + `ContentTargetFolders=content` → csproj gerçek assembly içermiyor, sadece `content/` klasörü
- `SuppressDependenciesWhenPacking=true` — template paketi bağımlılık taşımıyor (dotnet new SDK'sı yeter)
- Template'in kendi `CleanCore.Template.csproj`'u `<Content Include>`'dan exclude edildi — aksi halde üretilen projede kalırdı

**Bitiş notu:** v1.0 tam — `dotnet pack` → `.nupkg` → NuGet.org'a push ile ready. CI pipeline build/test/pack zincirini otomatikleştiriyor. Docker image Alpine + non-root, shared hosting/VPS uyumlu.

---

## Session notları

Kendi seansın bittiğinde buraya tarih + 1-2 satır düş. "Kaldığım yer" anlaşılsın.

### 2026-04-23
- **Faz 1–6 tamamlandı. CleanCore v1.0 hazır.**
- Faz 5: JWT Bearer + BCrypt + Refresh Token Rotation + HttpCurrentUser + AuthController. `refresh_tokens` migration'ı oluşturuldu.
- Faz 6: `dotnet new` template (shortName `cleancore`) + `CleanCore.Template.csproj` (NuGet pack) + multi-stage Dockerfile (Alpine, non-root) + GitHub Actions CI (build/test + pack on main).
- Lokal olarak `dotnet new install .` → `dotnet new cleancore -n DemoApi` test edildi, üretilen proje 0 warning/0 error derliyor ve 27 testi geçiyor. `--DbProvider SqlServer` parametresi de doğrulandı.
- Nupkg üretildi: `artifacts/CleanCore.Template.1.0.0.nupkg` (~68 KB). NuGet.org'a push için hazır.
- **Kaldığım yer:** v1.0 bitti. Sıradaki adımlar `docs/ROADMAP.md`'de (outbox, email, rate limiting, OpenTelemetry, role-based auth vs).
- **Yayın için:**
    1. `dotnet pack CleanCore.Template.csproj --configuration Release --output ./artifacts`
    2. `dotnet nuget push artifacts/CleanCore.Template.1.0.0.nupkg --api-key <KEY> --source https://api.nuget.org/v3/index.json`
    3. Kullanım: `dotnet new install CleanCore.Template` → `dotnet new cleancore -n MyApp`

### 2026-04-23 — Kod kalite revizyonu (SOLID / KISS / YAGNI taraması)
Kapsamlı tarama sonrası yapılan temizlik. Tek seferde uygulanan değişiklikler:
- **Domain events kaldırıldı** — `AggregateRoot`'tan event collection + Raise/Clear API, `User.Create`'deki `RaiseDomainEvent` çağrısı, `IDomainEvent` + `DomainEvent` + `UserRegisteredEvent` dosyaları, `UserConfiguration`'daki `Ignore(u => u.DomainEvents)`. YAGNI: dispatch mekanizması yoktu, feature yarım duruyordu. Outbox ile tam implementasyon `docs/ROADMAP.md`'de.
- **`DomainException` kaldırıldı** — hiç fırlatılmıyordu; `GlobalExceptionHandler.MapException`'daki case de silindi. Gerçek ihtiyaç doğduğunda eklenecek.
- **`AuthErrors.InactiveUser` + `AuthErrors.InvalidCredentials` kaldırıldı** — `UserErrors.Inactive` ve `UserErrors.InvalidCredentials` tek kaynak. `AuthErrors` sadece `InvalidRefreshToken` tutuyor (auth-flow'a özgü).
- **`ApiControllerBase.Sender` → `Mediator`** — MediatR doğrudan ifade ediliyor, junior dev için daha açık.
- **`AddAppHealthChecks` → `AddApiHealthChecks`** — "App" prefix'i anlamsızdı.
- **`LoginCommandHandler` — email enumeration koruması** — `user is null || !Verify(...)` satırı `credentialsValid` named local variable'a çıkarıldı, niyet okunur oldu.
- **Build:** 0 warning / 0 error. **Tests:** 27/27 yeşil (22 unit + 5 integration).
