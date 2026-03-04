using CleanCore.Application.Abstractions.Data;
using CleanCore.Domain.Shared;
using CleanCore.Domain.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CleanCore.Application.Users.GetUserById;

// =============================================================================
// GetUserByIdQueryHandler — read-only sorgu handler'ı
// =============================================================================
// Read path optimizasyonları:
//   - `AsNoTracking()`: change tracker kapalı. Read-only sorgularda tracking
//     hem bellek hem CPU'ya gereksiz yük bindirir.
//   - `Select(...)` projection: sadece DTO için gereken kolonları çek.
//     `User` aggregate'i 12 kolon var, DTO 5 — ağ trafiği ve allocation kazancı.
//
// Neden AutoMapper yok?
//   Manuel `Select` daha okunaklı, stack trace temiz, debug kolay.
//   AutoMapper'ın "convention magic"i küçük projede maliyetinden değerli değil.
//   50+ DTO'ya çıkınca Mapster (source-generated, MIT) eklenebilir.
//
// Cache mantığı:
//   Şu an yok. Önbellek (Hybrid Cache .NET 9+) eklenirse buraya entegre olur:
//     IMemoryCache + 30sn TTL → kullanıcı bilgileri sık değişmez, düşen istekleri kurtarır.
//   Roadmap'te.
// =============================================================================
internal sealed class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, Result<UserDto>>
{
    private readonly IApplicationDbContext _context;

    public GetUserByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<UserDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == request.UserId)
            .Select(u => new UserDto(
                u.Id,
                u.Email,
                u.FullName,
                u.IsActive,
                u.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
            return UserErrors.NotFound;

        return user;
    }
}
