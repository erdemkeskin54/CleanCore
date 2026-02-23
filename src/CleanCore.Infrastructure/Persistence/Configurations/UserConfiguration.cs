using CleanCore.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanCore.Infrastructure.Persistence.Configurations;

// =============================================================================
// UserConfiguration — `users` tablosu şeması
// =============================================================================
// Fluent API tercih sebebi (Data Annotation yerine):
//   - Domain entity'leri EF attribute'larıyla kirlenmesin (Domain → 0 paket bağımlılığı)
//   - Tüm config tek dosyada okunabilir
//   - DbContext.OnModelCreating'de assembly scan ile otomatik uygulanıyor
//
// İndeks notları:
//   `Email` UNIQUE — login lookup için zaten gerekli + duplicate'ı DB seviyesinde engelle.
//   Soft delete query filter (ApplicationDbContext.ApplySoftDeleteQueryFilter) email
//   uniqueness'i etkilemez — silinmiş kullanıcı için bile email rezerve kalır. İleride
//   "soft delete sonrası email reuse" istersen ya partial unique index (PG) ya da
//   custom check eklemek gerekir.
//
// `users` (lowercase) tablo adı:
//   Postgres convention'ı lowercase + snake_case. SQL Server case-insensitive
//   olduğu için ona da uygun. Migration generator default olarak
//   property adlarını kullanırdı ("Email" yerine bilinçli `users` seçimi).
// =============================================================================
internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        // Email max 256: RFC 5321'de teorik max 320 char ama pratikte hiçbir email
        // bu kadar uzun değil — 256 dünya standardı.
        builder.Property(u => u.Email)
            .HasMaxLength(256)
            .IsRequired();

        // Lookup hızı + uniqueness garantisi.
        builder.HasIndex(u => u.Email).IsUnique();

        // BCrypt hash ~60 char ama gelecek-proof için 512 — algoritma değişirse
        // (Argon2id hash daha uzun) migration zorlamamak için.
        builder.Property(u => u.PasswordHash)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(u => u.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(u => u.IsActive);

        // Audit (IAuditableEntity) — interceptor dolduruyor
        builder.Property(u => u.CreatedAt);
        builder.Property(u => u.CreatedBy).HasMaxLength(256);
        builder.Property(u => u.UpdatedAt);
        builder.Property(u => u.UpdatedBy).HasMaxLength(256);

        // Soft delete (ISoftDeletable) — interceptor delete'i UPDATE'e çeviriyor
        builder.Property(u => u.IsDeleted);
        builder.Property(u => u.DeletedAt);
        builder.Property(u => u.DeletedBy).HasMaxLength(256);
    }
}
