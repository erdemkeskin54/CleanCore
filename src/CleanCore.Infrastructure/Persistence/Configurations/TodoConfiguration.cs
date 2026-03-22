using CleanCore.Domain.Todos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanCore.Infrastructure.Persistence.Configurations;

// =============================================================================
// TodoConfiguration — `todos` tablosu schema'sı
// =============================================================================
// UserConfiguration ve RefreshTokenConfiguration ile aynı pattern.
// Yeni entity eklediğinde bu dosyayı template olarak kopyalayıp uyarla.
//
// EF Core ApplyConfigurationsFromAssembly otomatik tarıyor (ApplicationDbContext.OnModelCreating)
// → DbContext'e elle kayıt etmeye gerek yok, dosyayı doğru klasöre koymak yeter.
//
// İndeks stratejisi:
//   - UserId index → "kullanıcının todo'larını çek" sık sorgu (GetMyTodos handler'ı)
//   - Title üzerinde index YOK → arama özelliği şimdilik yok, eklendiğinde GIN/full-text düşünülür
// =============================================================================
internal sealed class TodoConfiguration : IEntityTypeConfiguration<Todo>
{
    public void Configure(EntityTypeBuilder<Todo> builder)
    {
        builder.ToTable("todos");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.UserId).IsRequired();
        // Sık sorgu: kullanıcının kendi todo'ları → index ile O(log n).
        builder.HasIndex(t => t.UserId);

        builder.Property(t => t.Title).HasMaxLength(200).IsRequired();
        builder.Property(t => t.IsCompleted);

        // Audit (IAuditableEntity) — interceptor dolduruyor
        builder.Property(t => t.CreatedAt);
        builder.Property(t => t.CreatedBy).HasMaxLength(256);
        builder.Property(t => t.UpdatedAt);
        builder.Property(t => t.UpdatedBy).HasMaxLength(256);

        // Soft delete (ISoftDeletable) — interceptor DELETE'i UPDATE'e çeviriyor + global query filter
        builder.Property(t => t.IsDeleted);
        builder.Property(t => t.DeletedAt);
        builder.Property(t => t.DeletedBy).HasMaxLength(256);
    }
}
