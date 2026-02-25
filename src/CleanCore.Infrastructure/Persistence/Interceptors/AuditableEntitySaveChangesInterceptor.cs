using CleanCore.Application.Abstractions.Services;
using CleanCore.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CleanCore.Infrastructure.Persistence.Interceptors;

// =============================================================================
// AuditableEntitySaveChangesInterceptor — Created/Updated alanlarını otomatik doldur
// =============================================================================
// SaveChanges'tan **önce** çalışır; ChangeTracker'da `IAuditableEntity` implement
// eden entity'leri tarar, state'ine göre Created/Updated alanlarını set eder.
//
// Niye interceptor, niye `SaveChanges` override değil?
//   - Interceptor DI-friendly: ICurrentUser, TimeProvider ctor'a inject edilir
//   - DbContext'i şişirmiyor — tek sorumluluk dosyada
//   - Test edilebilir: izole çalıştırılabilir
//   - Chain'lenebilir: SoftDeleteInterceptor'la beraber kayıt edilince ikisi de devreye giriyor
//
// "who" değeri:
//   `ICurrentUser.Email ?? "system"`. JWT yoksa (background job, seed) "system" damgalanır.
//   Email yerine UserId yazmak da mümkün — email log'da daha okunaklı geldi.
//
// Owned type değişimi:
//   Parent entity'de `User.Address` gibi owned ValueObject varsa, sadece adres
//   değişse de parent Modified sayılmalı (audit'e değişiklik yansısın).
//   `HasOwnedEntityChanges` bunu kontrol ediyor.
// =============================================================================
public sealed class AuditableEntitySaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public AuditableEntitySaveChangesInterceptor(ICurrentUser currentUser, TimeProvider timeProvider)
    {
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        ApplyAudit(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAudit(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAudit(DbContext? context)
    {
        if (context is null) return;

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var who = _currentUser.Email ?? "system";

        foreach (var entry in context.ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.CreatedBy = who;
            }
            else if (entry.State == EntityState.Modified || HasOwnedEntityChanges(entry))
            {
                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedBy = who;
            }
        }
    }

    // EF Core "owned types" parent'in property'si gibi davranır ama ChangeTracker'da
    // ayrı entry olarak görünür. Owned değişti ama parent property'leri değişmediyse
    // parent State == Unchanged kalır → biz manuel tetikliyoruz UpdatedAt'i set etsin.
    private static bool HasOwnedEntityChanges(EntityEntry entry) =>
        entry.References.Any(r =>
            r.TargetEntry is not null
            && r.TargetEntry.Metadata.IsOwned()
            && (r.TargetEntry.State == EntityState.Added || r.TargetEntry.State == EntityState.Modified));
}
