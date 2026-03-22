using CleanCore.Domain.Auth;
using CleanCore.Domain.Todos;
using CleanCore.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace CleanCore.Application.Abstractions.Data;

// Handler'lar bu interface üzerinden DB'ye erişir.
// Infrastructure'daki ApplicationDbContext'i direkt kullanmazlar — seam burada.
// NOT: Application katmanı EF Core'a bağımlı. Bu bilinçli bir taviz — DbSet ve
// LINQ uzantıları olmadan handler'lar okunaksız olur. Detay: docs/ARCHITECTURE.md
//
// Yeni feature eklerken: bu interface'e DbSet<Yeni> { get; } satırı ekle.
// Aksi halde handler'lar DbSet'e erişemez. ApplicationDbContext'te de aynı satırı eklemen gerekir.
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Todo> Todos { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
