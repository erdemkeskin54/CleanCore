using System.Linq.Expressions;
using CleanCore.Application.Abstractions.Data;
using CleanCore.Domain.Abstractions;
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
// Soft-delete query filter expression-tree ile kuruluyor:
//   ISoftDeletable implement eden her entity için `e => !e.IsDeleted` lambda'sı
//   model build sırasında runtime'da inşa ediliyor. Yeni entity eklenince filter
//   otomatik dahil — "unuttum" hatası imkansız.
//
// İleride: RefreshToken DbSet'i auth feature'ı geldiğinde eklenecek.
// =============================================================================
public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Tüm IEntityTypeConfiguration<T> sınıflarını otomatik uygula.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // ISoftDeletable entity'ler için global query filter (WHERE IsDeleted = false).
        ApplySoftDeleteQueryFilter(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

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
