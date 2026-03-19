using System.Linq.Expressions;
using CleanCore.Application.Abstractions.Data;
using CleanCore.Domain.Abstractions;
using CleanCore.Domain.Auth;
using CleanCore.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace CleanCore.Infrastructure.Persistence;

// =============================================================================
// ApplicationDbContext — EF Core DbContext + IApplicationDbContext seam
// =============================================================================
// Neden IApplicationDbContext'i **burada** implement ediyoruz?
//   - Application katmanı handler'ları `IApplicationDbContext`'i bilir, concrete DbContext'i bilmez.
//   - Test'te `IApplicationDbContext`'i fake'leyebiliriz; production'da bu concrete class sağlanır.
//   - DI tarafında: AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>()).
//
// EF Core ikilemi: DbContext zaten Unit of Work (SaveChanges) ve DbSet zaten Repository.
// `IApplicationDbContext` bir "adapter interface" görevi görüyor — saf bir repository katmanı
// eklemek yerine, mevcut EF Core API'sini tip güvenli ve test edilebilir şekilde expose ediyor.
// =============================================================================
public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Tüm IEntityTypeConfiguration<T> sınıflarını otomatik uygula.
        // Böylece yeni entity eklenince config'i scan ediliyor — DbContext'e dokunmaya gerek yok.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // ISoftDeletable entity'ler için global query filter ekle (WHERE IsDeleted = false).
        ApplySoftDeleteQueryFilter(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    // -------------------------------------------------------------------------
    // Global soft-delete query filter — niye expression tree ile kuruluyor?
    // -------------------------------------------------------------------------
    // Her `ISoftDeletable` entity için şu filter'ı kurmak istiyoruz:
    //     .HasQueryFilter(e => !e.IsDeleted)
    //
    // Ama generic method çağrılmayı derleme zamanında bilmiyoruz — entity tipleri runtime'da
    // geliyor. Bu yüzden expression tree'yi elle inşa ediyoruz:
    //     e  ← parameter:  Expression.Parameter(entityType, "e")
    //     e.IsDeleted  ← property:  Expression.Property(parameter, "IsDeleted")
    //     !e.IsDeleted ← not:  Expression.Not(property)
    //     lambda ← Expression.Lambda(notDeleted, parameter)
    //
    // EF Core bu lambda'yı model'e attach eder; her SELECT'e otomatik WHERE IsDeleted = false ekler.
    //
    // Alternatif (basit ama tekrarlı): Her entity config'inde manuel:
    //     builder.HasQueryFilter(u => !u.IsDeleted);
    // Bu yaklaşım: entity eklemeyi unutursan soft delete sessizce çalışmıyor. Runtime bug.
    // Expression-tree yaklaşımı: ISoftDeletable implement eden her şey otomatik dahil.
    // -------------------------------------------------------------------------
    private static void ApplySoftDeleteQueryFilter(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
                continue;

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var property = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
            var notDeleted = Expression.Not(property);
            var lambda = Expression.Lambda(notDeleted, parameter);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
        }
    }
}
