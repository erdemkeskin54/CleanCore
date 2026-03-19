using CleanCore.Domain.Auth;
using CleanCore.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace CleanCore.Application.Abstractions.Data;

// Handler'lar bu interface üzerinden DB'ye erişir.
// Infrastructure'daki ApplicationDbContext'i direkt kullanmazlar — seam burada.
// NOT: Application katmanı EF Core'a bağımlı. Bu bilinçli bir taviz — DbSet ve
// LINQ uzantıları olmadan handler'lar okunaksız olur. Detay: docs/ARCHITECTURE.md
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
