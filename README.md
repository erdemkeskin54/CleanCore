<div align="center">

# CleanCore

**.NET 10 Clean Architecture Web API Template**

Tek komutla kurulan, paylaşımlı hosting'de bile koşan, senior-level bir iskelet sunan üretime hazır bir Web API template'i.

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-13-239120?logo=csharp&logoColor=white)](https://docs.microsoft.com/dotnet/csharp/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql&logoColor=white)](https://www.postgresql.org/)
[![Docker](https://img.shields.io/badge/Docker-Alpine-2496ED?logo=docker&logoColor=white)](https://www.docker.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![CI](https://img.shields.io/badge/CI-GitHub%20Actions-2088FF?logo=githubactions&logoColor=white)](.github/workflows/ci.yml)
[![Tests](https://img.shields.io/badge/tests-27%20passing-brightgreen)](tests/)
[![Build](https://img.shields.io/badge/build-0%20warning%20%2F%200%20error-success)](#)

</div>

```bash
dotnet new install CleanCore.Template
dotnet new cleancore -n MyApp
```

---

> **Kime?** Clean architecture'ı abartıya kaçmadan, üretim ortamına atılabilecek bir iskeletle başlamak isteyenlere.
>
> **Ne değil?** Microservice template değil, event-sourcing değil, multi-tenant SaaS değil. Monolit bir Web API.
>
> **Neden var?** Aynı setup'ı her yeni projede sıfırdan kurmamak için — production'a açılacak işlere düz başlangıç noktası.

---

## İçindekiler

- [Stack](#stack)
- [Hızlı başlangıç](#hızlı-başlangıç)
- [Proje yapısı](#proje-yapısı)
- [Mimari kararlar & tradeoff'lar](#mimari-kararlar--tradeofflar)
  - [1. Genel iskelet](#1-genel-iskelet)
  - [2. Application katmanı](#2-application-katmanı)
  - [3. Data katmanı](#3-data-katmanı)
  - [4. API katmanı](#4-api-katmanı)
  - [5. Güvenlik](#5-güvenlik)
  - [6. Test stratejisi](#6-test-stratejisi)
  - [7. Tooling & paket seçimleri](#7-tooling--paket-seçimleri)
- [Template parametreleri](#template-parametreleri)
- [Docker](#docker)
- [CI/CD](#cicd)
- [Yol haritası](#yol-haritası)
- [Lisans](#lisans)

---

## Stack

| Kategori | Seçim | Versiyon | Neden? |
|---|---|---|---|
| Runtime | **.NET** | 10.0 | LTS, platform-bağımsız, son C# özellikleri |
| Web | ASP.NET Core + Controllers | 10.0 | Minimal API yerine Controller — büyük projede daha düzenli |
| ORM | EF Core + Npgsql/SqlServer | 10.0.x | Migration + interceptor olgunluğu, .NET ekosisteminin standardı |
| DB default | **PostgreSQL** | 16 (Alpine) | Açık kaynak, jsonb güçlü, her hosting'de var |
| Mediator | **MediatR** | 12.4.1 | Son Apache-2.0 majoru — v13+ ticari lisans, riskten kaçınıyoruz |
| Validation | FluentValidation | 12 | Pipeline behavior'a takılıyor, attribute'tan daha güçlü |
| Auth | **JWT Bearer + Refresh Rotation + BCrypt** | — | OAuth 2.0 BCP refresh rotation; password için BCrypt work factor 11 |
| Docs | Swashbuckle.AspNetCore | 6.8.1 (pinned) | .NET 10'un OpenApi 2.x breaking change'i nedeniyle pin edildi |
| Versioning | Asp.Versioning.Mvc | 10.0.0 | URL segment (`/api/v1/`) — header/query'den daha açık |
| Logging | **Serilog** | 10.0.0 | Structured logging, sink ekosistemi zengin |
| Tests | xUnit + plain `Assert` | — | FluentAssertions v8+ ticari → sadece BCL Assert |
| Container | Docker (Alpine, non-root, multi-stage) | — | ~120 MB image, güvenlik best-practice |
| CI | GitHub Actions | — | Build + test + pack zinciri |

---

## Hızlı başlangıç

### Yeni proje oluştur
```bash
dotnet new install CleanCore.Template     # Yayınlandıktan sonra. Şu an lokalden: dotnet new install .
dotnet new cleancore -n MyApp
cd MyApp
```

### Çalıştır
```bash
docker compose up -d                       # Postgres 16
dotnet run --project src/MyApp.Api         # Migration otomatik uygulanır + API ayağa kalkar
```

Swagger: <http://localhost:5000/swagger>

> Migration'lar dev ortamında startup'ta otomatik çalışır (`Program.cs` → `MigrateAsync`). Manuel istersen:
> `dotnet ef database update --project src/MyApp.Infrastructure --startup-project src/MyApp.Api`

### Demo kullanıcı (dev'de hazır)

İlk `dotnet run` sonrası seeder otomatik bir demo user yaratır — login için ne kullanacağını hatırlamaya gerek yok:

| Alan | Değer |
|---|---|
| E-posta | `demo@cleancore.dev` |
| Şifre | `Demo1234!` |

Seeder idempotent (zaten varsa no-op) ve sadece dev'de çalışıyor (`Environment.IsDevelopment` + `Database.IsRelational` guard'lı). Production'da kapalı.

### End-to-end auth akışı

1. `POST /api/v1/auth/login` — yukarıdaki demo credential'larıyla → `accessToken` + `refreshToken`
2. Swagger → **Authorize** → `Bearer {accessToken}`
3. `GET /api/v1/users/{id}` → 200 (id'yi login response'undan ya da DB'den al)
4. `POST /api/v1/auth/refresh` — eskiyi revoke, yeni pair al. Eski token artık 401.
5. Yeni kullanıcı için: `POST /api/v1/users` (anonim) — `email`, `password`, `fullName`

---

## Proje yapısı

```
src/
├── CleanCore.Domain/          → Entity, AggregateRoot, ValueObject, Result, Error   (bağımlılık: yok)
├── CleanCore.Application/     → CQRS handler'lar, validator, DTO, abstraction'lar    (bağımlılık: Domain)
├── CleanCore.Infrastructure/  → EF Core, JWT, BCrypt, interceptors                   (bağımlılık: Application)
└── CleanCore.Api/             → Controller, middleware, Program.cs                   (bağımlılık: hepsi)

tests/
├── CleanCore.UnitTests/         → Domain + Application handler + hash testleri       (22 test)
└── CleanCore.IntegrationTests/  → WebApplicationFactory + EF InMemory                (5 test)
```

Kural: **Domain hiçbir NuGet paketine bağımlı değil**, saf C#. Bu Clean Architecture'ın tek değişmez kuralı — kalanı bağlama göre esnetilebilir.

---

## Mimari kararlar & tradeoff'lar

> Her kararda aynı yapı: **Karar → Neden → Alternatifler → Tradeoff.** Saf teori değil, seçimin **neye mal olduğunu** yazıyor.
> Bu bölüm aynı zamanda kod içindeki yorum bloklarının özeti — her dosyaya kararın gerekçesi gömülü, README sadece haritası.

### 1. Genel iskelet

#### 1.1 Clean Architecture (4 katman, soğan değil kare)

**Karar:** Domain ← Application ← Infrastructure ← Api sırasıyla tek yönlü bağımlılık. Infrastructure servisleri interface'lerini Application'da tanımlıyor, implementasyonu Infrastructure'da.

**Neden:**
- Business logic (Domain + Application) framework'ten bağımsız — .NET'ten, EF'ten, ASP.NET'ten. Upgrade yolu açık.
- Test edilebilir: handler'ı fake `IApplicationDbContext` ile koşturursun, HTTP server lazım değil.
- Jason Taylor / Microsoft template'lerinin yaklaşımı — yeni birinin anlaması kolay.

**Alternatifler:**
- **Tek proje, "Services/" + "Models/"** — 4y CRUD dev'in tanıdığı klasik yapı. Küçükte hızlı, büyüdükçe dosyalar birbirine giriyor.
- **Vertical slice only** — katman yok, sadece `Features/CreateUser/` altında her şey. Çok pragmatik ama Domain modelini disipline tutmak zorlaşır.
- **Hexagonal / Onion** — isimler farklı, bağımlılık yönü aynı. Bizim yaklaşımımız pratikte hexagonal.

**Tradeoff:**
- 4 csproj kurulumu + proje referansları, CRUD dev için "niye bu kadar dosya lan" hissi. Öğrenme eğrisi ilk haftada dik.
- Kazancı: 6 ay sonra Stripe'ı İyzico'yla değiştirirken Infrastructure'daki tek dosyayı değiştirmek yetiyor.

---

#### 1.2 Multi-tenant **değil**

**Karar:** Tek veritabanı, tek şema. Tenant concept yok.

**Neden:**
- Paylaşımlı hosting'de tenant-per-schema imkansız (schema yetkisi yok).
- Tenant routing middleware'i production bug'larının 1 numaralı kaynağı: yanlış tenant'a yazmak = data leak.
- Küçük/orta projede gereksiz — 100 müşteri tek DB'de yaşar.

**Alternatifler:** (gerektiğinde eklenebilir — `docs/ROADMAP.md`'de)
- **Tenant-column:** `TenantId` kolonu + EF global query filter. En ucuz yöntem.
- **Tenant-schema:** Her tenant'a ayrı schema. İzolasyon iyi, migration operasyonu cehennem.
- **Tenant-database:** Her tenant'a ayrı DB. Tam izolasyon, hosting maliyeti katlanır.

**Tradeoff:** Multi-tenant bir ürün yazacaksan bu template **başlangıç noktası değil**. Baştan tenant-column ile başlamak daha ucuz.

---

#### 1.3 Vertical slice (her use case kendi klasöründe)

**Karar:** `Users/CreateUser/` altında `Command + Validator + Handler` üç dosya.

**Neden:**
- Feature'a müdahale ederken tek klasöre bakıyorsun.
- "Services/UserService.cs içinde 30 metot" probleminin panzehri.
- Rename/refactor IDE'de dert olmuyor.

**Alternatifler:**
- **Tek dosya:** `CreateUser.cs` içinde Command + Validator + Handler. Daha konsolide ama 100+ satır olunca yorucu.
- **Service sınıfı:** `UserService.CreateAsync` — klasik yaklaşım. Büyüdükçe "god class" riski.

**Tradeoff:** 20 use case = 20 klasör + 60 dosya. File tree uzun. Ama her biri kısa ve tekil sorumlulukta.

---

### 2. Application katmanı

#### 2.1 MediatR + CQRS

**Karar:** Her request (Command/Query) ayrı sınıf, MediatR `ISender.Send` ile dispatch.

**Neden:**
- Controller ince kalıyor: `return (await Mediator.Send(cmd)).ToActionResult();`
- Pipeline behaviors (validation, logging, unhandled exception) tek merkezde.
- Write (Command) ile Read (Query) ayrı sınıflar — farklı tuning, farklı projection kolayca.

**Alternatifler:**
- **Service katmanı:** `IUserService.CreateAsync(...)`. Tanıdık ama pipeline behavior tooling'i yok.
- **Minimal API + handler delegate:** Daha az ceremoni, ama FluentValidation + logging + exception entegrasyonu manuel.
- **Martinothamar Mediator (source-generated):** MIT, daha hızlı, lisans kaygısı yok. Ekosistem daha küçük.
- **Custom mediator (~30 satır):** Lisans/paket bağımlılığı yok. Pipeline yazımı ek iş.

**Tradeoff:** 1 request = 3 dosya. Basit bir read için de bu ceremoni. Ama disiplin getiriyor, takım projesinde faydası her dosyada görülüyor.

---

#### 2.2 MediatR **v12.4.1** (en güncel değil)

**Karar:** v13+ değil, v12.4.1 pin.

**Neden:**
- v13'ten itibaren MediatR **ticari lisans** modeline geçti. Template NuGet'e çıkacak → kullanıcı farkında olmadan lisans yükümlülüğü altına girmesin.
- v12.4.1 son Apache-2.0 majoru, feature setinde kayıp yok: `IRequest`, `INotification`, pipeline behaviors hepsi var.

**Alternatifler:**
- **MediatR v13+** — şirketin ticari lisansı varsa.
- **Mediator (martinothamar)** — MIT, source generator, daha hızlı. API benzer ama aynı değil.
- **Custom** — bağımlılık sıfır, yazımı bir saat.

**Tradeoff:** v12'nin güncelleme almadığı bir gelecekte alternatife geçmek gerekebilir. İstersen şimdi Mediator'a geç — interface zaten `IRequest<T>` gibi standart, değişim minimal.

---

#### 2.3 Result pattern (exception ile control flow değil)

**Karar:** Business hataları `Result.Failure(error)`. Exception sadece gerçek arızalarda (DB down, null config).

**Neden:**
- Exception throw + catch performans maliyetli, stack trace'i yanıltıcı.
- Bir handler imzasına bakınca "burası nasıl patlar" net: `Result<Guid>` döndürüyorsa business error var, throw ediyorsa panic.
- HTTP status mapping deterministik: `ErrorType` → 404/400/409/401/403/500 tek merkezde.

**Alternatifler:**
- **Her yerde exception:** C# ekosisteminin klasiği. Basit ama hangi exception'ın business hangisinin arıza olduğunu çözmek için her handler'ı okumak gerek.
- **OneOf / discriminated union:** F# / Rust zihniyeti. `Result<User, Error>` yerine `OneOf<User, NotFound, Conflict>`. Tip güvenliği daha yüksek, kütüphane/generic verbosity'si yüksek.
- **FluentResults paketi:** Daha zengin `Result` tipi. Ek bağımlılık, öğrenme.

**Tradeoff:** Handler kodu biraz daha verbose. Her use case iki yol kontrol ediyor: validation (throw) + business (return). Ama yol ayrı olduğu için okunabilirlik kazanıyor.

---

#### 2.4 Validation exception, business error Result — ikisini **karıştırmıyoruz**

**Karar:**
- **Validation hatası** (request malformed) → `ValidationException` fırlat → middleware 400 ProblemDetails.
- **Business error** (user not found, email exists) → `Result.Failure(err)` → controller `ToActionResult()` → 404/409.

**Neden:** İkisi farklı şey. Validation request'in handler'a hiç ulaşmaması gereken durumu, business error request'in geçerli olup iş kuralına takılması durumu.

**Alternatif:** Validation hatasını da `Result.Failure` olarak dönmek mümkün — o zaman `FluentValidation` entegrasyonunun behavior'ı değişmeli, pipeline daha karışık olur.

**Tradeoff:** Okuyucu iki mekanizmayı öğrenmek zorunda. Ama Jason Taylor / Ardalis template'lerinde de bu standart — yabancı kalmıyor.

---

#### 2.5 Pipeline behavior sırası: **Unhandled → Logging → Validation → Handler**

**Karar:** `AddOpenBehavior` ile tam bu sırada kayıt.

**Neden:** MediatR'da kayıt sırası = pipeline sırası. En dıştaki en önce kaydedilen.
- Unhandled dışta → her istisna yakalanıyor (ValidationException bilinçli olarak hariç).
- Logging onun içinde → başarılı ve validation-fail istekler de loglanıyor.
- Validation en içte → handler sadece valid request görüyor.

**Alternatif:** Transaction behavior da eklenir genelde (en içe, handler hemen dışına). Biz şimdilik eklemedik — handler'lar zaten `SaveChangesAsync` çağırıyor ve o interceptor'larla çalışıyor.

**Tradeoff:** Sıra görsel olarak kodda görünmüyor (kayıt satırlarına bakmak lazım). `docs/ARCHITECTURE.md` bu sırayı açıklıyor.

---

#### 2.6 AutoMapper **yok**

**Karar:** DTO projection'ı manuel: `.Select(u => new UserDto { ... })`.

**Neden:**
- Stack trace düzgün, debug kolay, "sihir" yok.
- `record` + `with` syntax zaten mapping'i kısaltıyor.
- AutoMapper setup + convention öğrenme + config hataları küçük projede kazancından fazla maliyet.

**Alternatifler:**
- **AutoMapper:** 50+ DTO olan büyük projelerde boilerplate'i azaltır.
- **Mapster:** Daha hızlı, source-generated versiyonu var, MIT.
- **ValueInjecter:** Eskidi, önerilmez.

**Tradeoff:** 50+ DTO'ya ulaşırsan mapping boilerplate yorucu olabilir. O noktada Mapster eklemek dakikalar sürüyor — retrofit kolay.

---

### 3. Data katmanı

#### 3.1 Generic `Repository<T>` **yok**, `IApplicationDbContext` abstraction'ı

**Karar:** Application handler'ları `IApplicationDbContext`'e bağlı (`DbSet<User>`, `SaveChangesAsync`). Repository interface katmanı yok.

**Neden:**
- EF Core zaten Unit of Work (DbContext) + Repository (DbSet) pattern'i. Üzerine bir katman daha koymak redundant.
- LINQ ifadelerini (`Include`, `Where`, `GroupBy`) repository metoduna taşımak çirkin oluyor.
- Jason Taylor / MS CleanArchitecture template'leri de bu yaklaşımı kullanıyor.

**Alternatifler:**
- **Generic repository:** `IRepository<T>` + LINQ expression parametreleri. Abstraction saf ama her metoda `Expression<Func<T, bool>>` geçirmek yorucu.
- **Spesifik repository:** Aggregate başına `IOrderRepository` — karmaşık yüklemeler (`GetFullOrderWithItems`) için anlamlı.

**Tradeoff:** `IApplicationDbContext` interface'i `Microsoft.EntityFrameworkCore` paketini Application katmanına sokuyor — "Application EF'ten bağımsız olacaktı" prensibi ihlal. Pragmatik seçim: saflık yerine okunabilirlik.

**Ne zaman gerçek repository ekle:** Aggregate'i tüm ilgili entity'leriyle yükleyen karmaşık query'ler çıkınca domain-spesifik repository yaz — generic değil.

---

#### 3.2 Interceptor'lar (audit + soft delete), `SaveChanges` override **değil**

**Karar:** `AuditableEntitySaveChangesInterceptor` + `SoftDeleteInterceptor` — iki ayrı interceptor DI ile.

**Neden:**
- Her biri tek sorumluluk.
- DI-friendly: `ICurrentUser` ve `TimeProvider` ctor'a inject ediliyor.
- Test edilebilir, izole.
- Chain'lenebilir: daha fazla cross-cutting concern gelirse yeni interceptor eklersin.

**Alternatifler:**
- **SaveChanges override:** DbContext şişer, test zorlaşır.
- **Domain event dispatch burada:** Event'leri `SaveChangesAsync` sırasında dispatch etmek mümkün (şu an event'ler raise ediliyor ama dispatch edilmiyor). Outbox pattern roadmap'te.

**Tradeoff:** İki interceptor'ı register etmeyi unutma = hata sessiz. DI kaydında sabit tutuldu, `AddInfrastructure` içinde.

---

#### 3.3 Soft delete: global query filter (expression tree) + interceptor

**Karar:** Her `ISoftDeletable` entity için otomatik `EntityTypeBuilder.HasQueryFilter(e => !e.IsDeleted)` — model build sırasında expression tree ile. Ek olarak `SoftDeleteInterceptor` DELETE operation'ını UPDATE IsDeleted=true'ya çeviriyor.

**Neden:**
- Global query filter sayesinde `Where(e => !e.IsDeleted)` her query'ye manuel yazmak zorunda değilsin.
- Silme kararını Repository'de değil altyapıda tutmak, "unuttum" bug'ını imkansız kılıyor.

**Alternatifler:**
- **Manuel `Where(e => !e.IsDeleted)`:** Her query'de hatırlamak. Bir gün unutursun.
- **Global filter yok, hard delete:** Audit/recovery kaybı. Muhasebe vb. alanlarda problem.

**Tradeoff:** Silineni görmek istediğinde `IgnoreQueryFilters()` yazman lazım. Admin paneli senaryolarında bu eklenebilir.

---

#### 3.4 PostgreSQL default (SQL Server toggle)

**Karar:** `appsettings.json` → `"Database:Provider": "Postgres"` default. SQL Server isteğe bağlı.

**Neden:**
- Ücretsiz, açık kaynak, her hosting veriyor.
- `jsonb` desteği güçlü (EF Core JsonColumn'u Postgres tarafında native).
- Npgsql provider olgun, aktif bakımda.
- TR'de paylaşımlı hosting'in çoğu MySQL veriyor — ama PG VPS'lerde ve cloud'da yaygın, gelişme yolu daha açık.

**Alternatifler:**
- **SQL Server default:** .NET ekosisteminde en yaygın. Enterprise ortamda konforlu. Lisans + hosting maliyeti.
- **SQLite:** Çok küçük projelerde. Write concurrency ve feature setinde Postgres seviyesinde değil.
- **MySQL/MariaDB:** Yaygın ama EF Core provider'ı Pomelo topluluğu tarafından bakılıyor (Microsoft değil).

**Tradeoff:** Başka provider eklemek istersen (MySQL) — `AddInfrastructure`'a yeni toggle + paket eklemek gerek.

---

#### 3.5 Migrations Infrastructure projesinde

**Karar:** `src/CleanCore.Infrastructure/Persistence/Migrations/` — migration'lar Infrastructure'da.

**Neden:** EF Core `DbContext`'in olduğu projede migration üretmek default ve doğru. Ayrı `Migrations` projesi açmak overkill.

**Alternatif:** Ayrı `CleanCore.Migrations` projesi — multi-context veya farklı deployment akışında faydalı. Tek context varsa gereksiz.

**Tradeoff:** Infrastructure projesi restore edilmeden migration komutu çalışmıyor. `--startup-project src/CleanCore.Api` parametresiyle bu normalleşiyor.

---

#### 3.6 `TimeProvider` (custom `IDateTimeProvider` yok)

**Karar:** .NET 8+ built-in `TimeProvider` sınıfı. Audit interceptor `TimeProvider.System`'den okuyor.

**Neden:**
- BCL'de mevcut — ek interface yazmaya gerek yok.
- Test: `Microsoft.Extensions.TimeProvider.Testing` paketinden `FakeTimeProvider`.
- `services.AddSingleton(TimeProvider.System)` tek satır.

**Alternatifler:**
- **Custom `IDateTimeProvider`:** Ancient pattern. Artık boilerplate.
- **Direkt `DateTime.UtcNow`:** Test edilemez.

**Tradeoff:** `TimeProvider` .NET 8 öncesinde yok. .NET 10 template için sorun değil.

---

### 4. API katmanı

#### 4.1 `IExceptionHandler` (.NET 8+), custom middleware değil

**Karar:** `GlobalExceptionHandler : IExceptionHandler` + `app.UseExceptionHandler()`.

**Neden:**
- DI-friendly: logger ctor'a geliyor.
- `ValidationException` → 400 + `errors` field dictionary (FluentValidation uyumlu).
- Diğer → 500, ProblemDetails standardı (RFC 7807).

**Alternatif:** Custom `ExceptionHandlingMiddleware` — eski .NET sürümlerinden kalma. Manuel response write, test daha zor.

**Tradeoff:** `IExceptionHandler` .NET 8 öncesinde yok. .NET 10 template için tam uyum.

---

#### 4.2 URL segment versioning (`/api/v1/...`)

**Karar:** `Asp.Versioning.Mvc` + `[ApiVersion("1.0")]` + `[Route("api/v{version:apiVersion}/...")]`.

**Neden:**
- URL'de görünür — curl/Postman'de açık.
- Caching proxy'ler versiyonları ayrı cache'ler.
- Swagger UI'da per-version doc zarifçe görünüyor.
- Route constraint ile compile-time kontrol.

**Alternatifler:**
- **Header (`Api-Version: 1.0`):** URL'de görünmüyor, test/debug zor.
- **Query (`?api-version=1.0`):** URL'de ama "utility parameter" gibi duruyor.
- **Hiç versioning:** MVP aşamasında idare eder, breaking change'te bedeli ağır.

**Tradeoff:** URL'de `/v1/` taşımak route'u biraz uzatıyor. Karşılığında versiyon disiplini pekişiyor — template zaten v1 ile geliyor, ikinci versiyon geldiğinde `v2` eklemek yeterli.

---

#### 4.3 `Result<T>.ToActionResult()` extension — controller'da switch yok

**Karar:** `Error.Type` → HTTP status mapping tek yerde (`ResultExtensions.ToActionResult`).

| ErrorType | HTTP |
|---|---|
| NotFound | 404 |
| Validation | 400 |
| Conflict | 409 |
| Unauthorized | 401 |
| Forbidden | 403 |
| Failure (diğer) | 500 |

**Neden:** Her controller'da `if/else` yazmak yorucu. Hata tipi → HTTP mapping merkezileşince tutarlılık garantileniyor.

**Alternatif:** Her controller kendi response'unu kuruyor. Esnek ama API'de tutarsızlık riski.

**Tradeoff:** Yeni bir HTTP status gerekirse `ErrorType` enum + mapping'i aynı anda güncellemek lazım. Eşgüdüm yeri belli, zor değil.

---

#### 4.4 Serilog + CorrelationId middleware

**Karar:** Serilog host-level config'ten okuyor, `CorrelationIdMiddleware` `X-Correlation-Id` header'ını LogContext'e koyuyor.

**Neden:**
- Structured logging default. JSON sink'e takmak config değişikliği meselesi.
- CorrelationId her log satırında: bir request'in tüm log'larını grep'lemek kolay.

**Alternatifler:**
- **Built-in `ILogger<T>`:** Structured ama sink ekosistemi Serilog kadar zengin değil.
- **NLog:** Aktif, ama Serilog kadar "property-first".

**Tradeoff:** Ekstra paket. Ama Serilog neredeyse endüstri standardı — CV'de "Serilog biliyorum" yazmak işe yarıyor.

---

#### 4.5 Swashbuckle **6.8.1**, en güncel değil

**Karar:** `Microsoft.AspNetCore.OpenApi` kaldırıldı, `Swashbuckle.AspNetCore 6.8.1` pin.

**Neden:**
- .NET 10 default template'i `Microsoft.AspNetCore.OpenApi 10.x` → `Microsoft.OpenApi 2.x` çekiyor.
- `Microsoft.OpenApi 2.x` breaking change: `OpenApiSecurityScheme.Reference` kaldırılmış.
- Swashbuckle 10.1.7 henüz 2.x API'sına tam uyumlu değil — JWT security scheme kurarken compile error.
- Swashbuckle 6.8.1 `Microsoft.OpenApi 1.x` ile uyumlu, olgun, stabil.

**Alternatifler:**
- **NSwag:** Alternatif Swagger/OpenAPI lib'i. Aynı ekosistem sıkıntısı yaşıyor olabilir.
- **Microsoft.AspNetCore.OpenApi tek başına:** Swagger UI'sı yok, sadece JSON endpoint.
- **Yeni Swashbuckle release'ini beklemek:** Geldiğinde yükseltiriz.

**Tradeoff:** 6.8.1'de minör özellikler eksik olabilir (filter'lar, yeni attribute'lar). Ancak JWT Bearer auth + per-version doc çalışıyor, üretime hazır.

---

### 5. Güvenlik

#### 5.1 JWT Bearer + **Refresh Token Rotation**

**Karar:** Access token 15dk, refresh token 7 gün, **rotation açık**: her refresh eskiyi revoke + yeni pair üretiyor.

**Neden (rotation):**
- Normal refresh: token çalınırsa saldırgan 7 gün erişim, tespit yok.
- Rotation'lı: eski token ikinci kez kullanılırsa **401** → detection. Saldırgan ilk kez kullanır, gerçek kullanıcı ikinci kez kullanır → ikisi de geçersiz, kullanıcı relogin'e zorlanır, saldırı ortaya çıkar.
- IETF OAuth 2.0 BCP önerisi. Endüstri standardı.

**Alternatifler:**
- **Rotation yok, uzun refresh:** Basit ama güvensiz.
- **Sadece short-lived access token (refresh yok):** UX'te sürekli relogin. Güvenli ama kullanıcı kaçar.
- **OAuth2 third-party provider (Auth0 / IdentityServer):** Feature zengin, ücretli veya kurulumu ağır.

**Tradeoff:** Rotation'ın handler'ı atomic olmalı (concurrent refresh race). Şu an basit implementasyonda single-use check yeterli — gerçek production'da `SELECT FOR UPDATE` eklenebilir.

---

#### 5.2 Refresh token DB'de **SHA256 hash**, plain değil

**Karar:** Client düz token taşır, DB'de sadece SHA256 hash. Lookup hash üzerinden.

**Neden:**
- DB leak olsa bile saldırgan aktif token'lardan plain versiyona dönemez (SHA256 tek yönlü).
- Password hashing'de BCrypt, refresh token'da SHA256 yeterli: refresh token zaten 512-bit random — brute force imkansız, wordlist saldırısı anlamsız.

**Alternatifler:**
- **Plain text saklama:** Bir DB leak = tüm aktif session'lar. Çok riskli.
- **BCrypt (password gibi):** Her login'de hash doğrulama ~100ms, refresh endpoint'inde yavaşlık. Over-engineering — refresh token'lar random, brute force yüzeyi yok.

**Tradeoff:** Kullanıcı token'ını kaybederse geri getirmenin yolu yok (DB'de plain yok). Bu zaten doğru davranış.

---

#### 5.3 Password hashing: **BCrypt** (work factor 11)

**Karar:** BCrypt.Net-Next, work factor 11 (~100ms hash).

**Neden:**
- Yavaş by design → brute force kullanışsız.
- Salt dahili, otomatik üretilir → rainbow table riski yok.
- Work factor artırılabilir: donanım hızlandıkça 12, 13'e çıkarılır.

**Alternatifler:**
- **SHA256:** Password için YANLIŞ — çok hızlı, salt yok, GPU saniyede milyarlarca dener.
- **Argon2id:** Memory-hard, GPU'lara karşı daha güçlü. .NET ekosisteminde paketleri daha az olgun.
- **PBKDF2:** BCL'de (`Rfc2898DeriveBytes`). Eski ama iyi. Work factor notion'ı zayıf.
- **scrypt:** Argon2'den eski versiyon, .NET'te BCrypt kadar yaygın değil.

**Tradeoff:** BCrypt 72-byte password limit'i var. 72+ karakterlik password neredeyse kimsenin kullanmayacağı bir durum ama teorik limit.

---

#### 5.4 `JwtSecurityTokenHandler.DefaultMapInboundClaims = false`

**Karar:** Claim adları olduğu gibi: `"sub"`, `"email"`, `"jti"`. Default URI mapping'i devre dışı.

**Neden:**
- Default açıkken JWT'deki `"sub"` claim'i `ClaimTypes.NameIdentifier` (uzun URI) olarak okunuyor. Token üretirken kısa, okurken uzun — bug kaynağı.
- Kapatınca: `JwtRegisteredClaimNames.Sub` (`"sub"`) hem token üretiminde hem okumada aynı sabit.
- `NameClaimType = Sub` ile `User.Identity.Name` de Sub'a bakıyor — tutarlılık.

**Alternatif:** Default açık bırakmak ve `ClaimTypes.*` sabitlerini kullanmak. İşler ama yazılması iki kat daha uzun ve kafa karıştırıcı.

**Tradeoff:** Default davranışı bekleyen legacy kod varsa kırar. Yeni template için sorun değil.

---

#### 5.5 Email enumeration koruması (login)

**Karar:** Kullanıcı yoksa veya şifre yanlışsa → aynı mesaj, aynı HTTP status: `401 Invalid credentials`. Timing farkını da korumak için non-existent user'da dummy BCrypt verify çağrısı (opsiyonel — şu an string equality ile biterse branch).

**Neden:**
- "Kullanıcı yok" vs "şifre hatalı" ayrımı: saldırgan hangi email'in kayıtlı olduğunu öğrenir. Brute force listesi daraltır.
- Doğru: her iki durumda aynı yanıt — saldırgan bilgi edinemez.

**Alternatif:** Register endpoint'inde "email already exists" döndürmek de enumeration'a açık. Çözüm: register'ı generic "Kayıt başarılı, email gelirse aktifleştir" flow'una çevirmek. Şu an bu flow'a girmedik — roadmap.

**Tradeoff:** Debug'da "şifrem yanlıştı sanırım" vs "email yanlış mı" UX'i karmaşık. Logging'e detaylı mesaj düşüyor, kullanıcıya generic.

---

#### 5.6 `RefreshToken`, `User` aggregate'inin parçası **değil** (ayrı entity)

**Karar:** Purist DDD'de RefreshToken User'ın child'ı olurdu. Biz ayrı tablo + FK yaptık.

**Neden:**
- RefreshToken'ın lifecycle'ı User'dan bağımsız: tek tek revoke ediliyor, expire oluyor, cleanup job'ı ayrı.
- User'ı her load'da refresh token'ları çekmek gereksiz overhead.

**Alternatif (purist):** `User.RefreshTokens` collection. Aggregate root User üzerinden her şey. Pattern temiz ama overhead ve karmaşıklık yaratıyor.

**Tradeoff:** RefreshToken'ı User'dan bağımsız yaratabilirsin — aggregate invariant'ını User enforce etmiyor. Fakat RefreshToken çok basit bir tekil entity, kimsenin aggregate invariant beklentisi yok.

---

#### 5.7 Role-based authorization **eklenmedi** (v1.0'da yok, roadmap'te)

**Karar:** Sadece `[Authorize]` var. Role/policy kurulumu yok.

**Neden:**
- Generic "admin/user" policy çoğu projede yetersiz — domain-spesifik policy yazmak gerek (ör. `UserCanEditOwnPost`).
- Template'e generic role middleware koymak, kullanıcının gerçek ihtiyaçla örtüşmeyen bir yapıyı kopyalamasına sebep.

**Alternatif:** Varsayılan olarak `[Authorize(Roles = "Admin")]` demo'su eklemek. Template'i şişirir, öğrenme zararı fayda ilişkisi zayıf.

**Tradeoff:** Kullanıcı authorization'ı kendisi eklemek zorunda. `docs/ROADMAP.md`'de "yüksek olasılık" listesinin başında.

---

### 6. Test stratejisi

#### 6.1 Unit test: EF Core InMemory provider

**Karar:** Handler unit testleri `Microsoft.EntityFrameworkCore.InMemory` ile. Fake `IPasswordHasher`, fake `ICurrentUser` manuel yazılıyor (Moq yok).

**Neden:**
- Hızlı (in-process), setup tek satır.
- Business logic doğrulanıyor, SQL davranışı değil.
- Mock kütüphanesi olmadan da basit fake'ler yazılabilir — kod daha okunaklı.

**Alternatifler:**
- **SQLite in-memory:** Gerçek SQL'e daha yakın (transaction semantiği). EF InMemory'den biraz daha yavaş, setup biraz daha fazla.
- **Testcontainers + Postgres:** Production'a en yakın. Setup süresi ~10sn, CI'da da çalışır ama daha yavaş.
- **Moq / NSubstitute:** Mock DbContext. Query mock'layabilir olmak için IQueryable kurulumu yorucu.

**Tradeoff:** InMemory provider gerçek SQL davranışını birebir karşılamıyor (case sensitivity, join planları, unique constraint). Bu sebeple integration test'te ayrıca WebApplicationFactory + InMemory çalışıyor — critical path'ler end-to-end doğrulanıyor.

---

#### 6.2 Integration test: `WebApplicationFactory<Program>` + izole InMemory service provider

**Karar:** `ConfigureTestServices` içinde InMemory EF Core — izole `IServiceProvider` ile. Npgsql servisleri shared DI'da kalıyor ama InMemory kendi izole provider'ında.

**Neden bu karmaşa:** Tipik yaklaşım `AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase())` — ama `AddInfrastructure` zaten Npgsql ekliyor. EF Core "iki provider tek DI'da kayıtlı" diye patlıyor.

**Çözüm:**
```csharp
static readonly IServiceProvider InMemoryServiceProvider =
    new ServiceCollection().AddEntityFrameworkInMemoryDatabase().BuildServiceProvider();

services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseInMemoryDatabase(_dbName);
    options.UseInternalServiceProvider(InMemoryServiceProvider);
});
```

**Alternatifler:**
- **Testcontainers + Postgres:** Provider sorunu yok, gerçek DB. CI'da Docker lazım.
- **Startup'ta provider'ı config-driven yapmak:** `AddInfrastructure`'ı test mode'a almak. API contract'ı test'in dikte etmesi kötü.

**Tradeoff:** Çözüm biraz sihirli görünüyor. `docs/ARCHITECTURE.md`'de detaylıca açıklandı — bakınca anlaşılıyor.

---

#### 6.3 `[assembly: InternalsVisibleTo("CleanCore.UnitTests")]`

**Karar:** Handler'lar `internal sealed class`. Test projesine `InternalsVisibleTo` ile görünür.

**Neden:**
- Handler'ı dışarıdan direkt instantiate etmek zararlı — her zaman MediatR üzerinden çalışsın.
- Production kodunun public surface'i küçük kalıyor.
- Test assembly'si özel erişim haklı — test kaçak bir tüketici değil.

**Alternatif:** Handler'ları public yapmak. Basit ama public surface şişer, "sakın çağırma" diye yorum yazmak zorunda kalırsın.

**Tradeoff:** Reflection ile erişen 3. parti araç varsa kırabilir. Şu an böyle bir bağımlılık yok.

---

### 7. Tooling & paket seçimleri

#### 7.1 `.slnx` yeni solution formatı

**Karar:** `CleanCore.slnx` — .NET 10 default'u.

**Neden:**
- Okunabilir XML. Eski `.sln` GUID'lerle dolu binary-ish format.
- Merge conflict çok daha az.
- VS 2022 17.10+, Rider 2024.3+ destekliyor. MSBuild native destek.

**Alternatif:** Eski `.sln` formatı. `dotnet sln -f Sln migrate` ile geri dönüş mümkün.

**Tradeoff:** Çok eski CI script'i `.sln` beklerse kırılabilir. Template tüm komutlarında `.slnx` kullanıyor.

---

#### 7.2 `.config/dotnet-tools.json` (lokal EF tool)

**Karar:** `dotnet-ef` global değil lokal tool manifest'te.

**Neden:**
- `dotnet tool restore` her ortamda aynı versiyonu getirir — CI reproducibility.
- Kullanıcı makinesinde global `dotnet-ef` yoksa çakışma yok.
- Template'in parçası, yeni projede bir komut yeterli.

**Alternatif:** Global install. "Benim makinemde çalışıyor" hikayelerinin kaynağı.

**Tradeoff:** Yeni kullanıcıya `dotnet tool restore` alışkanlığı öğretmek gerek. README'de var.

---

#### 7.3 FluentAssertions **yok**, plain `Assert`

**Karar:** xUnit'in kendi `Assert` sınıfı. FluentAssertions eklenmedi.

**Neden:**
- FluentAssertions v8'den itibaren ticari lisans. v7 hala MIT ama bir sonraki major lisansa takılırsa template'te bulunması uzun vadede sorun.
- Okunaklı method isimleri (`Should_Return_404_When_User_Not_Found`) + plain Assert çoğu zaman yeterli.

**Alternatifler:**
- **Shouldly:** MIT, aktif bakılıyor. İstersen kolayca eklenebilir.
- **NFluent:** Daha küçük kullanıcı kitlesi ama MIT.
- **FluentAssertions v7:** Hala MIT, sınırda.

**Tradeoff:** `collection.Should().Contain(...)` gibi expressif ifadeleri kaçırıyorsun. Karşılığında zero lisans riski.

---

#### 7.4 `TreatWarningsAsErrors` **kapalı**, ama `Nullable` **açık**

**Karar:** Warning'ler hata değil, ama null reference analizi aktif.

**Neden:**
- Template'ten üretilen projenin ilk build'i warning ile dolu çıkmasın — kullanıcı moral bozar.
- `Nullable` kapatmak = 2025+ olmaz. NRE bug'larını sessize almak demek.

**Alternatif:** Her ikisi de açık. "Senior codebase" hissi verir ama yeni kullanıcıyı bunaltır.

**Tradeoff:** Kullanıcı kendi projesinde `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` açabilir — bir satır.

---

#### 7.5 Docker: Alpine, multi-stage, non-root

**Karar:** `mcr.microsoft.com/dotnet/aspnet:10.0-alpine` + SDK build stage + `app` non-root user.

**Neden:**
- Alpine image ~120 MB — Debian-based'ten 300 MB küçük.
- Multi-stage: build artifact'ları runtime image'ına sızmıyor.
- Non-root: güvenlik best practice. Container escape zorlaşıyor.

**Alternatifler:**
- **Chiseled Ubuntu image** (MS'in yeni önerisi): daha da küçük, ama BusyBox utilities yok — debug etmek zor.
- **Debian slim:** 250+ MB ama glibc, bazı native lib'ler Alpine'da musl ile sıkıntı çıkarırsa Debian'a geç.

**Tradeoff:** Alpine musl libc kullanıyor, çok nadir durumlarda (bazı native lib'ler) problem yaratabilir. Standart .NET Web API'de sıfır sorun.

---

## Template parametreleri

| Parametre | Tip | Seçenekler / format | Default | Etkisi |
|---|---|---|---|---|
| `-n, --name` | string | proje adı | `MyApp` | `CleanCore` → `<Name>` namespace rename (tüm dosyalarda) |
| `--DbProvider` | choice | `Postgres`, `SqlServer` | `Postgres` | `appsettings.json`'da `Database:Provider` değerini değiştirir |
| `--JwtSigningKey` | string | ≥32 karakter | dev placeholder | `appsettings.json`'daki `Jwt:SigningKey` yerine gider |
| `--skipRestore` | bool | — | `false` | Template üretiminden sonra `dotnet restore` atmayı atla |

Örnek:
```bash
dotnet new cleancore -n AcmeApi --DbProvider SqlServer --JwtSigningKey "your-32-char-minimum-signing-key-here"
```

---

## Docker

```bash
docker build -t cleancore-api .
docker run --rm -p 8080:8080 \
  -e ConnectionStrings__Default="Host=host.docker.internal;Port=5432;Database=cleancore;Username=postgres;Password=postgres" \
  -e Jwt__SigningKey="your-production-signing-key-32char+" \
  cleancore-api
```

Image boyutu: ~120 MB (Alpine). Non-root kullanıcıda çalışır. Port 8080.

---

## Yeni feature nasıl eklenir? (Todo örneği)

Template'in her katmanını dolduran tam bir CRUD örneği `Todos/` altında. Kendi feature'ını eklerken aynı sırayı takip et — her dosya bir önceki katmanın bir parçasını ekler:

```
1. Domain          → src/CleanCore.Domain/Todos/Todo.cs               (aggregate root)
                  → src/CleanCore.Domain/Todos/TodoErrors.cs          (hata kodları)
2. Persistence    → src/CleanCore.Infrastructure/Persistence/Configurations/TodoConfiguration.cs
                  → IApplicationDbContext'e DbSet<Todo>
                  → ApplicationDbContext'e DbSet<Todo>
                  → dotnet ef migrations add AddTodos
                  → dotnet ef database update  (ya da `dotnet run` — auto-migrate)
3. Application    → src/CleanCore.Application/Todos/CreateTodo/{Command,Validator,Handler}.cs
                  → src/CleanCore.Application/Todos/GetMyTodos/{Query,Handler,Dto}.cs
                  → src/CleanCore.Application/Todos/ToggleTodo/{Command,Handler}.cs
                  → src/CleanCore.Application/Todos/DeleteTodo/{Command,Handler}.cs
4. API            → src/CleanCore.Api/Controllers/TodosController.cs  (POST/GET/PUT/DELETE)
```

**Dikkat edilmesi gerekenler:**

- **MediatR auto-scan**: Yeni handler eklediğinde DI değişmiyor — `Application/DependencyInjection.cs` içindeki `RegisterServicesFromAssembly` tüm `IRequestHandler<,>` implementasyonlarını otomatik buluyor.
- **Validator auto-scan**: `AddValidatorsFromAssembly` aynı şekilde — `<Command>Validator.cs` dosyasını koymak yeter.
- **Entity config auto-apply**: `ApplicationDbContext.OnModelCreating` → `ApplyConfigurationsFromAssembly` → yeni `<Entity>Configuration.cs` dosyası otomatik etkin.
- **Soft delete**: Entity `ISoftDeletable` implement ettiyse global query filter (DbContext'te expression tree ile kuruluyor) ve `SoftDeleteInterceptor` (DELETE → UPDATE) otomatik devrede.
- **Audit alanları**: `IAuditableEntity` implement edilmişse `AuditableEntitySaveChangesInterceptor` `CreatedAt/By + UpdatedAt/By`'i dolduruyor.
- **Authorization scope**: Token bazlı yetkilendirme (`[Authorize]`) kontroller seviyesinde; "kendi kaynağına erişim" handler içinde (`todo.UserId != currentUser.UserId → TodoErrors.NotOwner`).
- **Result pattern**: Business hatası `Result.Failure(error)`, exception sadece gerçek arıza için. Domain'den döndürdüğün `Error.Type` → controller'da `ResultExtensions.ToActionResult()` HTTP status'a maple.
- **Result vs Result\<T\>**: Generic `Result<T>` implicit cast (`return error;`) destekler, non-generic `Result` desteklemez — explicit `Result.Failure(error)` yaz.

Detay yorumlar her dosyanın başında. `Todos/` klasörü kopyalayıp adını değiştirip kendi domain'ine uyarlayabilirsin.

---

## CI/CD

`.github/workflows/ci.yml` iki job:

1. **build** — her push/PR'da: restore + build (Release) + test. Test `.trx` artifact'ları yükler.
2. **pack** — sadece `main` push'ta: `dotnet pack CleanCore.Template.csproj` → `.nupkg` artifact.

NuGet'e yayın manuel:
```bash
dotnet pack CleanCore.Template.csproj -c Release -o ./artifacts
dotnet nuget push artifacts/CleanCore.Template.1.0.0.nupkg \
  --api-key <NUGET_API_KEY> \
  --source https://api.nuget.org/v3/index.json
```

CI'a otomatik publish istersen `pack` job'una `dotnet nuget push` adımı eklenir — `NUGET_API_KEY` secret'ını aktif et.

---

## Yol haritası

v1.0 hazır. Yol haritasında olanlar (detay: [`docs/ROADMAP.md`](docs/ROADMAP.md)):

- Role / policy bazlı yetkilendirme örneği
- Outbox pattern — domain event'leri güvenilir teslim
- Rate limiting (.NET'in dahili rate limiter yapılandırması)
- OpenTelemetry ile dağıtık izleme
- E-posta servisi (SMTP / Resend / SendGrid adaptörleri)
- Dosya depolama (local / S3 / Azure Blob)
- Webhook handler (Stripe / İyzico idempotent iskelet)
- SignalR ile gerçek zamanlı bildirim
- Hybrid Cache (.NET 9+) — IMemoryCache + Redis kombine
- Feature flag desteği (config bazlı + LaunchDarkly opsiyonel)
- Multi-tenant opsiyonu (TenantId kolon yaklaşımı)
- i18n (resource dosyaları + culture middleware)

---

## Dokümantasyon

| Dosya | İçerik |
|---|---|
| [`docs/PROGRESS.md`](docs/PROGRESS.md) | Faz-faz checkbox — nereye nasıl geldiğimizin tarihçesi |
| [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) | Bu README'nin "Mimari kararlar" bölümünün uzun açıklamalı versiyonu |
| [`docs/FEATURES.md`](docs/FEATURES.md) | Uygulanan tüm özellikler + dosya referansları |
| [`docs/ROADMAP.md`](docs/ROADMAP.md) | v1.x fikirleri |

Yorum stratejisi: **her kritik dosyanın başında "neden bu şekilde" bloğu**. README zincirin haritası, kod yorumları detayı taşıyor.

---

## Katkı

Issue açabilirsin, PR atabilirsin. Geri bildirim de yeterli — gerekçeli "şu kararı sorgulayabilir miyiz" bile değerli.

---

## Lisans

MIT — kullan, fork'la, özelleştir. Credit verirsen çok iyi olur ama mecburiyet yok.

---

<div align="center">

**Erdem KESKİN** tarafından geliştirildi.
[@erdemkeskin54](https://github.com/erdemkeskin54)

Faydalı bulduysan ⭐ atmayı unutma.

</div>
