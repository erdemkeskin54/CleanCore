using CleanCore.Application.Abstractions.Authentication;
using CleanCore.Domain.Todos;
using CleanCore.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace CleanCore.Infrastructure.Persistence;

// =============================================================================
// DatabaseSeeder — dev ortamında demo kullanıcı + örnek todo'lar (idempotent seed)
// =============================================================================
// Niye ihtiyaç var?
//   `dotnet run` sonrası kullanıcı "ne ile login olayım" derdiyle karşılaşıyor.
//   Demo user + 3 örnek todo otomatik yaratılırsa Swagger'da login → access token →
//   /todos test → tek adımda full akış denenebiliyor.
//
// Idempotent: Demo user zaten varsa user oluşturmuyor. Demo user'ın todo'su varsa todo eklemiyor.
// Production'da çağrılmıyor — Program.cs'te `IsDevelopment` guard'ı var.
//
// Demo credential'ları:
//   E-posta : demo@cleancore.dev
//   Şifre   : Demo1234!
// (README'de de yazılı; geliştirici bunu kopyalayıp Swagger'da login'e yapıştırıyor.)
// =============================================================================
public static class DatabaseSeeder
{
    public const string DemoEmail = "demo@cleancore.dev";
    public const string DemoPassword = "Demo1234!";
    public const string DemoFullName = "Demo Kullanıcı";

    public static async Task SeedAsync(
        ApplicationDbContext db,
        IPasswordHasher hasher,
        CancellationToken cancellationToken = default)
    {
        var demoUser = await db.Users
            .FirstOrDefaultAsync(u => u.Email == DemoEmail, cancellationToken);

        if (demoUser is null)
        {
            demoUser = User.Create(DemoEmail, hasher.Hash(DemoPassword), DemoFullName);
            db.Users.Add(demoUser);
            await db.SaveChangesAsync(cancellationToken);
        }

        // Demo todo'lar — sadece demo user'ın hiç todo'su yoksa ekle (idempotent).
        // Mevcut todo'ları olan user için eklemiyoruz: geliştirici kendi todo'larıyla
        // çalışmış olabilir, üstüne tekrar seed atmak gürültü yaratır.
        var hasTodos = await db.Todos
            .AnyAsync(t => t.UserId == demoUser.Id, cancellationToken);

        if (!hasTodos)
        {
            db.Todos.AddRange(
                Todo.Create(demoUser.Id, "CleanCore'u dene"),
                Todo.Create(demoUser.Id, "Yeni bir feature ekle"),
                Todo.Create(demoUser.Id, "Frontend'i ayağa kaldır"));
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
