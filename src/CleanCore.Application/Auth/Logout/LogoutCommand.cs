using CleanCore.Domain.Shared;
using MediatR;

namespace CleanCore.Application.Auth.Logout;

// Refresh token'ı server tarafında revoke etmek için. Access token zaten kısa
// ömürlü (15dk), elle invalidate edemeyiz (stateless JWT). Refresh'i revoke
// edince yeni access alınamaz → kullanıcı 15dk içinde tamamen logout olmuş olur.
//
// İdeal logout client + server koordineli:
//   - Server: bu command ile refresh token revoke
//   - Client: localStorage/cookie'yi temizle
public sealed record LogoutCommand(string RefreshToken) : IRequest<Result>;
