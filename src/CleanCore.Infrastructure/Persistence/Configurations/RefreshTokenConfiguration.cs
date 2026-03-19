using CleanCore.Domain.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CleanCore.Infrastructure.Persistence.Configurations;

// =============================================================================
// RefreshTokenConfiguration — `refresh_tokens` tablosu şeması
// =============================================================================
// Tablo lifecycle:
//   - Issue: Login + Refresh handler INSERT
//   - Lookup: Refresh + Logout handler SELECT (TokenHash ile)
//   - Revoke: UPDATE (IsRevoked, RevokedAt) — Soft delete kavramından bağımsız
//
// İndeks stratejisi:
//   - `UserId` non-unique: kullanıcı başına çoklu cihazdan login → birden çok aktif token
//   - `TokenHash` UNIQUE: aynı plain token iki user'a verilemez (random'da çakışma
//     ihtimali astronomik, ama belt-and-suspenders)
//
// Foreign key bilinçli **YOK**:
//   - User soft-delete edildiğinde refresh token'ları "kaskadlı" silmek isteyebilir miyiz? Hayır.
//   - User pasif edildiğinde refresh token'lar pasif olur (handler `user.IsActive` kontrol).
//   - Kasıtsız orphan'a karşı temizlik: scheduled job (Hangfire/cron) tarafından eski + revoke'lu
//     token'ları silmek (90 gün sonra) iyi olur. Roadmap'te.
//
// Cleanup stratejisi (production):
//   Bu tablo sınırsız büyür çünkü her login yeni satır ekler. Schedule:
//     DELETE FROM refresh_tokens
//     WHERE (IsRevoked = true AND RevokedAt < NOW() - INTERVAL '30 days')
//        OR ExpiresAt < NOW() - INTERVAL '30 days'
//   Şu an manuel — Hangfire entegrasyonu yapılınca otomasyona alınacak.
// =============================================================================
internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.UserId).IsRequired();

        // Kullanıcı başına token sorgusu (token cleanup, multi-device logout vs).
        builder.HasIndex(t => t.UserId);

        // SHA256 hash base64 → ~44 char. 512 ileride farklı algoritma değişikliği için tampon.
        builder.Property(t => t.TokenHash)
            .HasMaxLength(512)
            .IsRequired();

        // En sık sorgu: refresh isteğinde "bu token DB'de var mı?" — UNIQUE INDEX hızlı.
        builder.HasIndex(t => t.TokenHash).IsUnique();

        builder.Property(t => t.ExpiresAt);
        builder.Property(t => t.CreatedAt);
        builder.Property(t => t.IsRevoked);
        builder.Property(t => t.RevokedAt);
    }
}
