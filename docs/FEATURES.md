# CleanCore — Özellikler

Uygulanan özellikler ve nerede olduklarına dair kısa notlar.
Her faz bitiminde buraya eklenir.

---

## Domain — `src/CleanCore.Domain/`

### Abstractions
| Tip | Dosya | Ne işe yarar? |
|---|---|---|
| `Entity` | `Abstractions/Entity.cs` | Id üzerinden eşitlenen domain objesi base class'ı. |
| `AggregateRoot` | `Abstractions/AggregateRoot.cs` | Transaction boundary marker. Domain event dispatch mekanizması outbox ile geri eklenecek (`docs/ROADMAP.md`). |
| `ValueObject` | `Abstractions/ValueObject.cs` | Değer bazlı eşitlik (Money, Email vb.). |
| `IAuditableEntity` | `Abstractions/IAuditableEntity.cs` | Created/Updated by/at alanları. |
| `ISoftDeletable` | `Abstractions/ISoftDeletable.cs` | IsDeleted + DeletedAt/By. |

### Shared
| Tip | Dosya | Ne işe yarar? |
|---|---|---|
| `Result` | `Shared/Result.cs` | Non-generic success/failure. |
| `Result<T>` | `Shared/Result.cs` | Generic: success + value veya failure + error. |
| `Error` | `Shared/Error.cs` | Code + Message + Type. HTTP mapping için Type kullanılır. |
| `ErrorType` | `Shared/Error.cs` | Failure, Validation, NotFound, Conflict, Unauthorized, Forbidden. |

### Users
| Tip | Dosya | Ne işe yarar? |
|---|---|---|
| `User` | `Users/User.cs` | AggregateRoot örneği, IAuditable + ISoftDeletable. Factory + davranış metotları. |
| `UserErrors` | `Users/UserErrors.cs` | **Tek merkez:** `NotFound`, `EmailAlreadyExists`, `InvalidCredentials`, `Inactive`. |

### Auth
| Tip | Dosya | Ne işe yarar? |
|---|---|---|
| `RefreshToken` | `Auth/RefreshToken.cs` | Hash olarak saklanan tek kullanımlık refresh token. `Issue`, `Revoke`, `IsActive` metotları. |

## Application — `src/CleanCore.Application/`

### Abstractions
| Tip | Dosya | Ne işe yarar? |
|---|---|---|
| `IApplicationDbContext` | `Abstractions/Data/IApplicationDbContext.cs` | Handler'ların DB erişim seam'i. `DbSet<User>` + `DbSet<RefreshToken>` + `SaveChangesAsync`. |
| `ICurrentUser` | `Abstractions/Services/ICurrentUser.cs` | "Şu an kim istek atıyor?" — audit ve authorization için. |
| `IPasswordHasher` | `Abstractions/Authentication/IPasswordHasher.cs` | Password hash + verify. BCrypt impl Infrastructure'da. |
| `IJwtProvider` | `Abstractions/Authentication/IJwtProvider.cs` | JWT access token üretir (sub/email/jti/name claim'leri). |
| `IRefreshTokenGenerator` | `Abstractions/Authentication/IRefreshTokenGenerator.cs` | Random refresh token + SHA256 hash. |
| `JwtOptions` | `Abstractions/Authentication/JwtOptions.cs` | Issuer/Audience/SigningKey/expiry config. |
| `AuthResponse` | `Abstractions/Authentication/AuthResponse.cs` | Login/Refresh dönüş tipi — access + refresh + expiry'ler. |

### Pipeline Behaviors
| Tip | Dosya | Ne işe yarar? |
|---|---|---|
| `ValidationBehavior<,>` | `Behaviors/ValidationBehavior.cs` | Validator varsa koştur, hata → `ValidationException` fırlat. |
| `LoggingBehavior<,>` | `Behaviors/LoggingBehavior.cs` | Request adı + elapsed ms log'u. |
| `UnhandledExceptionBehavior<,>` | `Behaviors/UnhandledExceptionBehavior.cs` | Beklenmedik exception'ı log'la, rethrow et. |

### Users (use case'ler)
| Tip | Dosya | Ne işe yarar? |
|---|---|---|
| `CreateUserCommand` | `Users/CreateUser/CreateUserCommand.cs` | `IRequest<Result<Guid>>` — Email/Password/FullName input. |
| `CreateUserCommandValidator` | `Users/CreateUser/CreateUserCommandValidator.cs` | Email format, password length, fullname zorunlu. |
| `CreateUserCommandHandler` | `Users/CreateUser/CreateUserCommandHandler.cs` | Email uniqueness + **password hash (BCrypt)** + save. |
| `GetUserByIdQuery` | `Users/GetUserById/GetUserByIdQuery.cs` | `IRequest<Result<UserDto>>` — Id input. |
| `UserDto` | `Users/GetUserById/UserDto.cs` | Read model — Id, Email, FullName, IsActive, CreatedAt. |
| `GetUserByIdQueryHandler` | `Users/GetUserById/GetUserByIdQueryHandler.cs` | `AsNoTracking()` + manuel `Select` projection. |

### Auth (use case'ler)
| Tip | Dosya | Ne işe yarar? |
|---|---|---|
| `AuthErrors` | `Auth/AuthErrors.cs` | Sadece auth-flow'a özgü: `InvalidRefreshToken`. Kullanıcı hataları `UserErrors`'ta. |
| `LoginCommand` | `Auth/Login/LoginCommand.cs` | `IRequest<Result<AuthResponse>>` — email + password. |
| `LoginCommandHandler` | `Auth/Login/LoginCommandHandler.cs` | Credential verify + JWT + refresh issue. Email enumeration koruma. |
| `RefreshTokenCommand` | `Auth/Refresh/RefreshTokenCommand.cs` | `IRequest<Result<AuthResponse>>` — refresh token. |
| `RefreshTokenCommandHandler` | `Auth/Refresh/RefreshTokenCommandHandler.cs` | **Rotation**: eskiyi revoke, yeni pair issue. |
| `LogoutCommand` | `Auth/Logout/LogoutCommand.cs` | Refresh token revoke (idempotent). |
| `LogoutCommandHandler` | `Auth/Logout/LogoutCommandHandler.cs` | Token'ı bul ve revoke et. |

### DI
| Tip | Dosya | Ne işe yarar? |
|---|---|---|
| `AddApplication` | `DependencyInjection.cs` | MediatR + FluentValidation + 3 behavior tek çağrıda kayıt. |

## Infrastructure — `src/CleanCore.Infrastructure/`

### Persistence
| Tip | Dosya | Ne işe yarar? |
|---|---|---|
| `ApplicationDbContext` | `Persistence/ApplicationDbContext.cs` | EF Core DbContext + otomatik soft delete query filter. |
| `UserConfiguration` | `Persistence/Configurations/UserConfiguration.cs` | `users` tablosu, unique email index, domain event ignore. |
| `AuditableEntitySaveChangesInterceptor` | `Persistence/Interceptors/` | `CreatedAt`/`CreatedBy`/`UpdatedAt`/`UpdatedBy` otomatik doldurur. |
| `SoftDeleteInterceptor` | `Persistence/Interceptors/` | DELETE → UPDATE IsDeleted=true çevirir. |
| `Migrations/InitialCreate` | `Persistence/Migrations/` | İlk migration — users tablosu + audit/soft delete kolonları + email unique index. |

### Services
| Tip | Dosya | Ne işe yarar? |
|---|---|---|
| `HttpCurrentUser` | `Services/HttpCurrentUser.cs` | `IHttpContextAccessor` ile JWT claim'den UserId/Email okur. |

### Authentication
| Tip | Dosya | Ne işe yarar? |
|---|---|---|
| `BCryptPasswordHasher` | `Authentication/BCryptPasswordHasher.cs` | BCrypt.Net-Next, work factor 11. |
| `JwtProvider` | `Authentication/JwtProvider.cs` | HmacSha256 imzalı JWT üretir. Claim: sub, email, jti, name. |
| `RefreshTokenGenerator` | `Authentication/RefreshTokenGenerator.cs` | 512-bit random + SHA256 hash. |

### DI
| Tip | Dosya | Ne işe yarar? |
|---|---|---|
| `AddInfrastructure` | `DependencyInjection.cs` | Postgres/SqlServer provider toggle, DbContext, interceptor, TimeProvider, ICurrentUser kayıtları. |

## Api — `src/CleanCore.Api/`

### Controllers
| Tip | Dosya | Ne işe yarar? |
|---|---|---|
| `ApiControllerBase` | `Controllers/ApiControllerBase.cs` | Tüm controller'ların base'i. Lazy `Mediator` (ISender) property. |
| `UsersController` | `Controllers/UsersController.cs` | `POST /users` (anon) + `GET /users/{id}` (**`[Authorize]`**). |
| `AuthController` | `Controllers/AuthController.cs` | `POST /auth/login`, `/auth/refresh`, `/auth/logout` (logout `[Authorize]`). |

### Middleware
| Tip | Dosya | Ne işe yarar? |
|---|---|---|
| `GlobalExceptionHandler` | `Middleware/GlobalExceptionHandler.cs` | `IExceptionHandler` impl. ValidationException → 400, diğer → 500. ProblemDetails. |
| `CorrelationIdMiddleware` | `Middleware/CorrelationIdMiddleware.cs` | `X-Correlation-Id` header + Serilog LogContext. |

### Extensions
| Tip | Dosya | Ne işe yarar? |
|---|---|---|
| `ResultExtensions` | `Extensions/ResultExtensions.cs` | `Result<T> → IActionResult`. Error.Type → HTTP status mapping. |
| `CorsConfigurationExtensions` | `Extensions/CorsConfigurationExtensions.cs` | `AddCorsPolicy`, config-driven origin listesi. |
| `ApiVersioningExtensions` | `Extensions/ApiVersioningExtensions.cs` | URL segment versioning (`/api/v1/`). |
| `SwaggerExtensions` | `Extensions/SwaggerExtensions.cs` | Swashbuckle + JWT Bearer button + per-version docs. |
| `HealthCheckExtensions` | `Extensions/HealthCheckExtensions.cs` | `AddApiHealthChecks` — Postgres/SqlServer provider'a göre. |
| `AuthenticationExtensions` | `Extensions/AuthenticationExtensions.cs` | `AddJwtAuthentication` — `JwtBearer` config'den, `DefaultMapInboundClaims=false`. |

### Pipeline (Program.cs)
1. `UseSerilogRequestLogging` → structured HTTP request log
2. `UseMiddleware<CorrelationIdMiddleware>` → request id
3. `UseExceptionHandler` → GlobalExceptionHandler devrede
4. Dev'de: `UseSwagger` + `UseSwaggerUI` → `/swagger`
5. `UseCors("Default")`
6. **`UseAuthentication`** → JWT Bearer doğrulama
7. `UseAuthorization`
8. `MapControllers`
9. `MapHealthChecks("/health")` + `MapHealthChecks("/health/ready")`

### Config
- `appsettings.json` → Serilog, ConnectionStrings, Database:Provider, Cors:AllowedOrigins, **Jwt (Issuer/Audience/SigningKey/expiry)**
- `appsettings.Development.json` → dev DB (`cleancore_dev`), EF Core SQL log, CORS origins

> ⚠️ **SigningKey production'da secret manager'dan gelsin.** appsettings'teki değer sadece dev/demo.

## Tests

### Unit Tests — `tests/CleanCore.UnitTests/`
| Dosya | Kapsam |
|---|---|
| `Domain/ResultTests.cs` | Result + Result<T> + implicit cast + Error factory (6 test) |
| `Domain/ValueObjectTests.cs` | Eşitlik + hashcode bazlı ValueObject davranışı (2 test) |
| `Application/CreateUserCommandValidatorTests.cs` | Email, password, fullname validation kuralları (6 test) |
| `Application/CreateUserCommandHandlerTests.cs` | Happy path + email conflict (2 test, fake password hasher + InMemory) |
| `Application/GetUserByIdQueryHandlerTests.cs` | Found + NotFound senaryoları (2 test, EF Core InMemory) |
| `Infrastructure/BCryptPasswordHasherTests.cs` | Hash/verify + farklı salt davranışı (4 test) |

### Integration Tests — `tests/CleanCore.IntegrationTests/`
| Dosya | Kapsam |
|---|---|
| `CleanCoreWebAppFactory.cs` | `WebApplicationFactory<Program>` test versiyonu. InMemory DB + izole internal service provider. |
| `Users/UsersControllerTests.cs` | Create invalid 400 + Get without token 401 + Create/Login/Get 200 flow (3 test) |
| `Auth/AuthControllerTests.cs` | Invalid login 401 + full register/login/refresh/rotation flow (2 test) |

**Toplam:** 27 test (22 unit + 5 integration), hepsi yeşil.

## Root

| Dosya | Ne işe yarar? |
|---|---|
| `docker-compose.yml` | Postgres 16 Alpine + healthcheck, `cleancore_dev` DB ile. |
| `Dockerfile` | Multi-stage (SDK build → aspnet runtime), Alpine, non-root `app` user, `ASPNETCORE_URLS=http://+:8080`. |
| `.dockerignore` | `bin/`, `obj/`, `docs/`, `tests/`, `.github/` vb. image'dan dışarıda. |
| `.config/dotnet-tools.json` | `dotnet-ef` CLI aracı bu projeye özel, restore ile otomatik gelir. |
| `.github/workflows/ci.yml` | `build` job (restore/build/test + trx artifact), `pack` job (main push'ta `.nupkg` üretir). |

## Template (NuGet)

| Dosya | Ne işe yarar? |
|---|---|
| `.template.config/template.json` | `dotnet new` template metadata — shortName `cleancore`, `sourceName: "CleanCore"` (namespace rename), `--DbProvider` / `--JwtSigningKey` / `--skipRestore` parametreleri. |
| `CleanCore.Template.csproj` | `PackageType=Template` pack projesi. Content-only (`IncludeBuildOutput=false`), bağımlılık taşımıyor (`SuppressDependenciesWhenPacking`). `docs/`, `.github/`, kendi csproj'u exclude. |

### Kullanım
```bash
# Yayınlandıktan sonra
dotnet new install CleanCore.Template
dotnet new cleancore -n MyApp --DbProvider Postgres

# Lokal geliştirme sırasında
dotnet new install .
dotnet new cleancore -n Demo -o /tmp/demo
dotnet new uninstall "C:\path\to\CleanAchitecture"
```

### Parametreler
| Ad | Tip | Seçenek | Default |
|---|---|---|---|
| `--DbProvider` | choice | `Postgres`, `SqlServer` | `Postgres` |
| `--JwtSigningKey` | string | ≥32 char | dev placeholder |
| `--skipRestore` | bool | — | `false` |
