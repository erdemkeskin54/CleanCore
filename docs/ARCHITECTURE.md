# CleanCore — Mimari Kararlar

Bu dosya **"niye bu şekilde yapıldı"** sorusunun cevaplarını tutar.
Bir kural koyarken buraya yaz. 6 ay sonra "niye böyle yapmışım" dediğinde cevap burada olacak.

---

## Katman Bağımlılıkları (Clean Architecture)

```
Api ──► Infrastructure ──► Application ──► Domain
                                   │              ▲
                                   └──────────────┘
```

| Katman | Neye bağlı? | Ne yapar? |
|---|---|---|
| **Domain** | Hiçbir şeye | Entity, ValueObject, Result. Saf C#, dış dünyadan haberi yok. |
| **Application** | Sadece Domain | Use case (CQRS). Interface tanımlar, implementasyon yok. |
| **Infrastructure** | Application + Domain | EF Core, JWT, email, cache — tüm dış dünya. |
| **Api** | Hepsine | HTTP layer. Controller ve middleware. |

**Altın kural:** Domain asla NuGet paketi referans almaz. Saf C#.

---

## Neden Multi-Tenant Değil?

Orijinal plan tenant-per-schema idi. Küçük/orta proje için overkill:
- Her tenant için ayrı migration koşmak operasyon cehennemi
- Paylaşımlı hosting'de schema yaratma yetkisi çoğu zaman yok
- Tenant routing middleware'i — yanlış tenant'a yazma, production bug'larının 1 numaralı kaynağı

**İleride lazım olursa:** `tenantId` column + EF global query filter. Schema değil.

---

## Neden Redis Opsiyonel?

Paylaşımlı hosting'de Redis ya yok ya da ek ücretli.
- Default: `IMemoryCache` (in-process)
- `appsettings.json` → `Cache:Provider = "Redis"` set edilirse `StackExchange.Redis` devreye girer
- İkisi de aynı `ICacheService` interface'ini implement eder (Application katmanında tanımlı)

---

## Neden MediatR / CQRS?

- Controller ince kalır: `return await Mediator.Send(command)` — tek satır
- Her use case kendi dosyasında — arama ve okuma kolay
- Pipeline behaviors ile validation, logging, transaction tek merkezde

**Kural:** Her use case için 3 dosya: `Command/Query`, `Handler`, `Validator`. Daha fazlası overengineering. `Handler` nested class da olabilir (aynı dosyada).

---

## Neden Result Pattern?

- Business hatalarında exception fırlatmak pahalı — "control flow as exception" anti-pattern
- `Result.Success(value)` / `Result.Failure(error)` → intent net
- Exception sadece **gerçek** hatalar için: DB down, network timeout, config eksik

**Kural:** Domain + Application normalde exception fırlatmaz — `Result.Failure(error)` döner. Validation hatası ise `FluentValidation` pipeline behavior'ı `ValidationException` fırlatır, global handler 400'e çevirir.

> **Not:** Daha önce `DomainException` base class'ı vardı ama hiç fırlatılmıyordu (YAGNI). Gerçek ihtiyaç doğduğunda (ör. DB concurrency conflict) geri eklenecek.

---

## Neden AutoMapper Yok?

- Manuel mapping: daha az büyü, daha okunaklı, stack trace düzgün
- Record + `with` ile mapping zaten kısa
- AutoMapper'ın setup + öğrenme maliyeti, küçük projelerde faydasından fazla

50+ DTO'ya ulaşırsak dönüş yaparız. Şimdilik yok.

---


## Neden PostgreSQL Default?

- Ücretsiz, open source, çoğu hosting sağlıyor
- JSON column desteği güçlü (`jsonb`)
- EF Core Npgsql provider olgun
- SQL Server'a geçiş: connection string + provider flag değiştirmek yetiyor (Faz 2'de bu toggle kurulacak)

---

## Neden `TreatWarningsAsErrors` Kapalı?

Template olduğu için yeni proje ayağa kalkarken warning'lere boğulmak kullanıcıyı üzer. Kapalı başlıyor, kullanıcı ihtiyacına göre açar.

Ama `Nullable` enabled — null reference bug'larını önlemenin tek yolu.

---

## Neden `.slnx` (`.sln` değil)?

`.NET 10`'dan itibaren `dotnet new sln` default olarak `.slnx` (XML formatı) üretiyor.

**Avantajlar:**
- Okunabilir, diff'i anlaşılır — eski `.sln` format binary-ish GUID'lerle doluydu
- Daha az merge conflict
- MSBuild native olarak destekliyor (.NET 10 SDK)

**Dikkat:** Eski CI script'leri `.sln` beklerse sorun çıkabilir. Bu template'de tüm komutlar `CleanCore.slnx` ile çalışıyor. VS 2022 (17.10+) ve Rider (2024.3+) destekliyor.

Eğer eski `.sln` formatına ihtiyaç olursa: `dotnet sln -f Sln migrate` komutuyla dönüşüm mümkün.

---


## Neden FluentAssertions Yok?

v8'den itibaren lisanslı hale geldi. v7 hâlâ MIT ama bir sonraki major release'de değişiklik olursa template'e eklenmiş olması uzun vadede sorun doğurur. Plain `xUnit.Assert` ile başlıyoruz, okunaklı isimlerde test metotları yeterli.

İstenirse kullanıcı kolayca `Shouldly` (MIT, hâlâ aktif) paketini ekleyebilir.

---

## Neden Generic `Repository<T>` Yok, Direkt `IApplicationDbContext`?

EF Core DbContext zaten **Unit of Work** pattern'i, `DbSet<T>` zaten **Repository** pattern'i. Üzerine bir `IRepository<T>` eklemek çoğunlukla redundant:
- LINQ ifadelerini kaybederiz (`.Include()`, `.Where(...)`, `.GroupBy()` — repository metodu olarak bunları taklit etmek çirkin)
- Test için fark yok — her ikisi de interface seam sağlıyor
- Jason Taylor / Microsoft templates bu yaklaşımı kullanıyor

**Taviz:** `IApplicationDbContext` interface'i Application katmanından `Microsoft.EntityFrameworkCore`'u referans etmeyi gerektiriyor. Saflıktan ödün, okunabilirlikten kazanç.

**Ne zaman gerçek repository eklenir?** Karmaşık bir aggregate'in tüm ilgili entity'lerini birlikte yükleyen (`GetFullOrderWithItems`) bir repository işine yarayabilir. O zaman Domain'e özel `IOrderRepository` yazılır, generic değil.

---

## Neden `TimeProvider` (Custom `IDateTimeProvider` Değil)?

`.NET 8`'den itibaren BCL'de `TimeProvider` abstract class'ı var. Tests için `Microsoft.Extensions.TimeProvider.Testing` paketindeki `FakeTimeProvider` mock sağlıyor.

Custom `IDateTimeProvider` yazmak gereksiz boilerplate. `services.AddSingleton(TimeProvider.System)` yeter.

---

## Neden Interceptor'lar, `SaveChanges` Override Değil?

İkisi de çalışır ama interceptor:
- DI friendly — `ICurrentUser`, `TimeProvider` inject edilebilir
- Test edilebilir — tek başına çalıştırılabilir
- Chain'lenebilir — audit + soft delete ayrı interceptor'lar, her biri tek sorumluluk

SaveChanges override DbContext'i şişirir, test zorlaşır.

---

## Neden `.config/dotnet-tools.json`?

`dotnet-ef` CLI'ı global yerine **lokal tool manifest** olarak kurduk. Avantaj:
- `dotnet tool restore` her ortamda aynı versiyonu getirir (CI reproducibility)
- Kullanıcı makinesinde global ef tool yoksa sorun yok
- Template'in bir parçası — yeni projede kullanıcı `dotnet tool restore` yazmak yeterli

`.config/` konumu Microsoft'un önerdiği standart yer.

---

## Neden Domain Events Mekanizması Yok (Şimdilik)?

Başlangıçta `AggregateRoot`'ta event collection + `RaiseDomainEvent`/`ClearDomainEvents` API'si, `UserRegisteredEvent` örneği vardı. Ama **dispatch mekanizması yoktu** — event'ler raise ediliyor ama hiçbir yere gönderilmiyordu. Yarım feature = kafa karışıklığı kaynağı.

**SOLID/YAGNI taraması sonrası karar:** Mekanizmayı tamamen kaldır. `AggregateRoot` şimdilik `Entity`'den farksız bir marker. İhtiyaç olduğunda (email gönderimi, webhook, cache invalidation) tam implementasyonla geri gelecek.

**Tam implementasyon geldiğinde ne olacak:**
- Raise/Clear API geri eklenecek
- `ApplicationDbContext.SaveChangesAsync` override'ında event'ler toplanıp **Outbox** tablosuna yazılacak (at-least-once delivery, retry, idempotency için)
- Background worker (Hangfire veya hosted service) outbox'ı okuyup MediatR `IPublisher.Publish` ile dispatch edecek
- Detay: `docs/ROADMAP.md` → "Outbox pattern"

**Neden in-memory dispatch değil?** SaveChanges'te event fırlatıp transaction dışı bir hizmete çağırmak = "DB commit oldu ama email gitmedi" veya tersi riski. Outbox bu consistency sorununu çözüyor.

Şu anki template "event'siz" çalışıyor — böylece junior dev boş/yarım bir pattern'le karşılaşmıyor.

---

## Neden MediatR 12.4.1 (en güncel değil)?

MediatR v13'ten itibaren ticari lisans modeline geçti. Template olarak NuGet'e atılacak bir proje için bu sorun — kullanıcılar farkında olmadan lisans yükümlülüğü edinmesin.

**v12.4.1** son Apache-2.0 majoru. Özellik olarak ne kaybediyoruz? Neredeyse hiçbir şey — IRequest, INotification, pipeline behaviors hepsi mevcut. CV'de "MediatR" yazınca da bu pattern'i tanıyan interviewer'lar zaten bu sürümü biliyor.

**Alternatifler (ileride lazım olursa):**
- [Mediator by martinothamar](https://github.com/martinothamar/Mediator) — MIT, source-generated (daha hızlı)
- Custom minimal mediator (~30 satır)
- MediatR v13+ (şirket lisansı varsa)

---

## Neden Pipeline Behavior Sırası: Unhandled → Logging → Validation?

MediatR'da `AddOpenBehavior`'la kayıt sırası = pipeline sırası. En dıştaki en önce kayıtlı olan.

```
Unhandled ───► Logging ───► Validation ───► Handler
    │              │             │
    │              │             └── validation hatası → ValidationException
    │              └── "handled in Xms" log'u
    └── her türlü beklenmedik exception'ı yakala + log'la (ValidationException hariç)
```

**Niye bu sıra?**
- Unhandled dışta → her şeyi yakalar, tek yerde error log'u
- Logging onun içinde → başarılı istekleri log'la, handler elapsed'ı ölç
- Validation en içte → handler'a sadece geçerli request'ler ulaşsın

---

## Neden Validation'da Exception, Business Error'da Result?

Validation hatası ≠ business error:
- **Validation hatası** = "request malformed" → HTTP 400. `ValidationException` fırlat, Faz 4 middleware'i 400 ProblemDetails'e çevirsin.
- **Business error** = "user not found" → HTTP 404. `Result.Failure(UserErrors.NotFound)` dön.

Bu ikili yaklaşım Jason Taylor/Ardalis template'lerinde standart. Kod iki hata türünü de temiz ayrı tutuyor.

---

## Neden Vertical Slice (Her Use Case Kendi Klasöründe)?

```
Users/
├── CreateUser/
│   ├── CreateUserCommand.cs
│   ├── CreateUserCommandValidator.cs
│   └── CreateUserCommandHandler.cs
└── GetUserById/
    ├── GetUserByIdQuery.cs
    ├── GetUserByIdQueryHandler.cs
    └── UserDto.cs
```

- Feature'a bir ekleme yaparken tek klasöre bakarsın
- Use case'in tüm parçaları bir arada
- Rename/refactor kolay
- "Services/UserService.cs" içinde 30 metot beklemenden çok daha okunaklı

**Alternatif:** Tek dosyaya `CreateUser.cs` içinde command + validator + handler. Daha da konsolide ama dosya 100 satırı geçince yorucu. Klasör + 3 dosya orta yol.

---

## Neden EF Core InMemory Provider'ı Handler Test'lerinde?

EF Core InMemory:
- ✓ Hızlı (in-process)
- ✓ Setup basit
- ✗ Gerçek SQL davranışı yok (join'ler, transaction semantiği, case sensitivity)

Unit test'lerde **business logic** doğrulanıyor, SQL davranışı değil. `AnyAsync`, `FirstOrDefaultAsync`, `Add` + `SaveChanges` — bunlar InMemory'de doğru çalışıyor.

**Gerçek DB testine ihtiyaç olursa** (unique constraint, concurrency vs.): Faz 4'te Integration test'lerde Testcontainers + Postgres kullanacağız.

---

## Neden `[assembly: InternalsVisibleTo]`?

Handler'lar `internal sealed class` — dışarıdan direkt instantiate edilmesin, hep MediatR üzerinden çağrılsın.

Ama test için erişim lazım. `[assembly: InternalsVisibleTo("CleanCore.UnitTests")]` ile test projesine görünür yap, production kod'una değil.

---

## Neden Swashbuckle 6.8.1, En Güncel 10.1.7 Değil?

.NET 10 SDK, default `webapi` template'ine `Microsoft.AspNetCore.OpenApi 10.x` paketini eklemeye başladı. O da `Microsoft.OpenApi 2.x`'i zorunlu kılıyor.

Ama:
- `Microsoft.OpenApi 2.x` breaking change yapmış — `OpenApiSecurityScheme.Reference` kaldırılmış, `OpenApiReference` class'ı yok
- Swashbuckle 10.1.7 henüz 2.x API'sına tam uyumlu değil — security scheme ayarlarken compile error
- Swashbuckle 6.8.1 `Microsoft.OpenApi 1.x` kullanıyor, eski (ama çalışan) API
- `Microsoft.AspNetCore.OpenApi`'yi de csproj'tan kaldırdık — Swashbuckle zaten OpenAPI doc üretiyor

**Sonuç:** Stabil Swashbuckle 6.8.1'e pin. Gelecekte Swashbuckle 2.x API'sına uyum sağlarsa yükseltilir.

---

## Neden `IExceptionHandler` (Middleware Değil)?

.NET 8+ `IExceptionHandler` interface'i, exception handling için pipeline-stage-aware, test edilebilir ve chain'lenebilir bir yol. Avantajlar:
- DI'dan gelenler ctor injection ile (logger, vs.)
- `app.UseExceptionHandler()` otomatik devreye alıyor
- Birden fazla handler kayıt edilebilir (ilki `TryHandleAsync` true dönen zincir kırar)

Custom middleware yazmak da çalışır ama DI + test edilebilirlik açısından `IExceptionHandler` tercih edilir.

---

## Neden `ResultExtensions.ToActionResult()` Var — Controller'da Switch Yazmadık?

Her controller'da `if (result.IsSuccess) ... else ...` yazmak yorucu. `result.ToActionResult()` tek satır — `Error.Type` üzerinden HTTP status mapping merkezi:

| ErrorType | HTTP |
|---|---|
| NotFound | 404 |
| Validation | 400 |
| Conflict | 409 |
| Unauthorized | 401 |
| Forbidden | 403 |
| Failure (diğer) | 500 |

Mapping kuralları bir yerde (Domain ErrorType → HTTP) → tutarlı API davranışı.

---

## Neden URL Segment Versioning (Query Header Değil)?

`/api/v1/users` — URL'de görünen versiyon. Avantajlar:
- Tarayıcıda/curl'de açık
- Caching proxy'ler farklı versiyonları ayrı cache'ler
- Route constraint ile compile time kontrol
- Swagger UI'da doğal görünüyor

Alternatifler:
- Header (`Api-Version: 1.0`) — URL'de görünmüyor, test/debug zor
- Query (`?api-version=1.0`) — URL'de ama "utility parameter" gibi, kirli

URL segment en anlaşılır ve endüstri standardı.

---

## Neden Integration Test'te InMemory İzole ServiceProvider Lazım?

`AddInfrastructure` Npgsql servislerini DI'ya ekliyor. Test'te InMemory'ye geçerken normal `AddDbContext` çağırınca iki provider da DI'da kalıyor → EF Core "Only a single database provider can be registered" hatası.

**Çözüm:** InMemory için ayrı, izole bir `IServiceProvider` inşa et ve `options.UseInternalServiceProvider(...)` ile ver:

```csharp
static readonly IServiceProvider InMemoryProvider =
    new ServiceCollection().AddEntityFrameworkInMemoryDatabase().BuildServiceProvider();

services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseInMemoryDatabase(_dbName);
    options.UseInternalServiceProvider(InMemoryProvider);
});
```

Böylece InMemory'nin EF Core internal servisleri kendi izole provider'ında, Npgsql'inkiler shared DI'da. Çakışma yok.

---

## Neden Refresh Token Rotation?

Normal refresh token (rotation yok):
- Client refresh token'ı 7 gün kullanabilir
- Token çalınırsa saldırgan da 7 gün erişim sağlar
- Token ne zaman çalındığı belli değil

**Rotation ile:**
- Her refresh → eski token revoke + yeni pair
- Eski token ikinci kez kullanılırsa → 401
- Saldırgan ilk kez kullandığında siteye girer AMA sonraki kullanıma kadar — sonra hem saldırganın hem gerçek kullanıcının token'ı invalid olur
- Bu noktada kullanıcı yeniden login ister → saldırı tespit edilmiş demektir

Detection + mitigation bir arada. Endüstri best practice (IETF OAuth 2.0 BCP).

---

## Neden Refresh Token DB'de SHA256 Hash'i?

Plain text refresh token'ı DB'de saklamak = password'ü plain text saklamak. DB leak'te bütün aktif session'lar kaybolur.

**Hash ile:**
- DB'de sadece hash var
- Client token'ı kendinde tutuyor, hash'lenerek DB'de lookup ediliyor
- DB leak'te saldırgan hash'lerden plain token'a geri dönemez (SHA256 tek yönlü)
- Brute force denemek de 512-bit entropy nedeniyle imkansız

Password'lerde BCrypt (slow hash, salted) kullanıyoruz ama refresh token'larda SHA256 (fast hash, no salt) yeterli:
- Refresh token zaten random — brute force için wordlist kullanılamaz
- Her refresh'te yeni token üretildiği için hash collision window'u çok dar

---

## Neden Password İçin BCrypt (SHA256 Değil)?

SHA256 password hashing için YANLIŞ seçim:
- Çok hızlı → saldırgan saniyede milyarlarca deneyebilir
- Salt yok (SHA256 kendiliğinden) → aynı password → aynı hash → rainbow table saldırısı

**BCrypt:**
- Yavaş (work factor 11 → ~100ms) → brute force kullanışsız
- Salt'ı kendi içinde üretir ve saklar (her hash farklı)
- Donanım hızlanırken work factor artırılabilir

Argon2 ve scrypt de iyi alternatifler ama BCrypt .NET'te en yaygın ve olgun.

---

## Neden `JwtSecurityTokenHandler.DefaultMapInboundClaims = false`?

Default davranış: JWT'deki `"sub"` claim'i `ClaimTypes.NameIdentifier` ("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier") URI'sine maplenir. Bu:
- Kodda ClaimTypes.* sabit URI'leri ile çalışmayı gerektirir
- Token üretirken "sub" yazıyorsun, okurken "ClaimTypes.NameIdentifier" ile arıyorsun
- Kafa karışıklığı + bug kaynağı

**Kapattığımızda:**
- JWT'de ne yazıyorsa o gelir: "sub", "email", "jti"
- JwtRegisteredClaimNames.Sub gibi standard sabitler ile eşleşir
- Tutarlılık

`NameClaimType = JwtRegisteredClaimNames.Sub` ile User.Identity.Name da Sub'a bakar — standart.

---

## Neden Login'de Email Enumeration Koruma?

Yanlış: `user == null → "Kullanıcı yok"`, `password yanlış → "Şifre hatalı"`. Saldırgan hangi email'in kayıtlı olduğunu öğrenir.

Doğru: Her iki durumda da aynı mesaj: **"Email veya şifre hatalı."** Saldırgan bilgi edinemez.

Implementation: Tek `if (user is null || !verify(password, user.PasswordHash))` bloku.

---

## Neden `RefreshToken`, `User` Aggregate'inin Parçası Değil?

DDD purist yaklaşım: RefreshToken, User aggregate'inin içinde olur (child entity).

**Ama:**
- RefreshToken başına buyruk bir lifecycle'a sahip — kullanıcıyla beraber güncellenmiyor
- User'a her load'da refresh token'ları çekmek gereksiz overhead
- RefreshToken'ları cleanup/expire ile yönetmek ayrı iş

**Pragmatik:** Ayrı tablo, ayrı entity, FK ile User'a bağlı. Purity'den kaçındık, basitlik + performans kazandık.
