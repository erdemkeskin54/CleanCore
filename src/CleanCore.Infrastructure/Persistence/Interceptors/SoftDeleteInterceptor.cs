using CleanCore.Application.Abstractions.Services;
using CleanCore.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CleanCore.Infrastructure.Persistence.Interceptors;

// =============================================================================
// SoftDeleteInterceptor — DELETE çağrısını UPDATE IsDeleted=true'ya çevir
// =============================================================================
// Çalışma mantığı:
//   Handler `_context.Users.Remove(user)` yazdığında EF ChangeTracker entry'yi
//   Deleted state'ine alır. Biz SaveChanges'tan önce bu entry'yi yakalar,
//   state'i Modified yapar ve IsDeleted/DeletedAt/DeletedBy alanlarını set ederiz.
//   Sonuçta DB'ye giden SQL DELETE değil, UPDATE olur — fiziksel silme yok.
//
// Niye interceptor, niye repository pattern içinde "soft delete" metodu değil?
//   - Repository'siz mimari (IApplicationDbContext) — kod direkt DbSet kullanıyor
//   - "Unutulabilir" hata: bir yerde Remove() çağrılırsa fiziksel silme yapardı
//   - Interceptor seviyesinde halledilince **unutmak imkansız**
//
// `ApplicationDbContext.ApplySoftDeleteQueryFilter` ile beraber çalışır:
//   - Filter: SELECT'lerde `WHERE IsDeleted = false` otomatik
//   - Interceptor: DELETE'i UPDATE IsDeleted = true'ya çeviriyor
//   İkisi birlikte: silinmiş kayıt görünmez + remove edilen kayıt fiziksel silinmez
//
// Hard delete istenirse?
//   `_context.Database.ExecuteSqlRawAsync("DELETE FROM users WHERE ...")` ile bypass edilir.
//   Veya context'te "ignoreSoftDelete" mode flag'i ekleyip interceptor kontrol eder.
//   Şu an için kasıtlı yok — soft delete'in tüm noktası "yanlışlıkla fiziksel silme" yapmamak.
// =============================================================================
public sealed class SoftDeleteInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public SoftDeleteInterceptor(ICurrentUser currentUser, TimeProvider timeProvider)
    {
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        ConvertDeletesToSoft(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ConvertDeletesToSoft(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ConvertDeletesToSoft(DbContext? context)
    {
        if (context is null) return;

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var who = _currentUser.Email ?? "system";

        foreach (var entry in context.ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State != EntityState.Deleted) continue;

            // State'i Deleted'tan Modified'a çekiyoruz: EF bu entry'i UPDATE olarak gönderecek.
            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
            entry.Entity.DeletedAt = now;
            entry.Entity.DeletedBy = who;
        }
    }
}
