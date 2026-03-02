using CleanCore.Application.Abstractions.Authentication;
using CleanCore.Application.Abstractions.Data;
using CleanCore.Domain.Shared;
using CleanCore.Domain.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanCore.Application.Users.CreateUser;

// =============================================================================
// CreateUserCommandHandler — yeni kullanıcı kaydı
// =============================================================================
// Akış:
//   1) Email normalize et (trim + lowercase) → User.Create de aynısını yapar, lookup tutarlı
//   2) AnyAsync ile email uniqueness kontrol → conflict ise 409
//   3) Password BCrypt ile hash
//   4) Domain factory `User.Create` ile entity yarat (id Guid.NewGuid())
//   5) DbContext'e ekle + SaveChanges
//   6) Yaratılan user.Id'yi dön
//
// Race condition (TOCTOU):
//   AnyAsync ile "email var mı?" kontrol + Add arasında milisaniyelik yarış var.
//   İki istek aynı email'le aynı anda gelirse: ikisi de "yok" görür, ikisi de Add eder.
//   DB'de UNIQUE INDEX (UserConfiguration) bu durumda ikinci insert'ü patlatır
//   (DbUpdateException → 500). UX'te 500 yerine 409 göstermek için handler'da
//   try/catch + duplicate key inspection eklenebilir. Şimdilik kabullenildi —
//   gerçek üretimde sık karşılaşılan bir durum değil, eklemek kolay.
//
// Şifre hash maliyeti:
//   BCrypt work factor 11 → ~100ms. CPU-bound. Yüksek registration throughput
//   olan senaryoda hash'i background queue'ya almak (hashing worker pool) düşünülür.
//   Burada yok — KISS, tipik kullanım için yeterli.
// =============================================================================
internal sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public CreateUserCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<Guid>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // User.Create de aynı normalizasyonu yapıyor — lookup'ta da aynı şekilde arıyoruz.
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var emailExists = await _context.Users
            .AnyAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (emailExists)
            return UserErrors.EmailAlreadyExists;

        // Hash hesaplama CPU-bound; await olmadan koşar (BCrypt sync API).
        // Concurrency'yi kaybetmemek için throughput çok yüksekse hashing'i background'a almak gerekir.
        var passwordHash = _passwordHasher.Hash(request.Password);
        var user = User.Create(normalizedEmail, passwordHash, request.FullName);

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}
