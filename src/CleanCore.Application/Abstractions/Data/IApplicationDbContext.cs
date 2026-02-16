using CleanCore.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace CleanCore.Application.Abstractions.Data;

// Handler'lar bu interface üzerinden DB'ye erişir.
// Infrastructure'daki ApplicationDbContext'i direkt kullanmazlar — seam burada.
// NOT: Application katmanı EF Core'a bağımlı. Bu bilinçli bir taviz — DbSet ve
// LINQ uzantıları olmadan handler'lar okunaksız olur. Detay: docs/ARCHITECTURE.md
//
// İleride: RefreshTokens DbSet'i auth feature'ı geldiğinde eklenecek.
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
