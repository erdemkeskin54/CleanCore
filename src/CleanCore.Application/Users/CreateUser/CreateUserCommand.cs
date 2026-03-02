using CleanCore.Domain.Shared;
using MediatR;

namespace CleanCore.Application.Users.CreateUser;

// Yeni kullanıcı kaydı. Şu an `[AllowAnonymous]` (bkz. UsersController) — ileride
// admin-only yapılırsa public register endpoint'i ayrı action olarak eklenir.
//
// Result<Guid> dönüyoruz — yaratılan user'ın Id'sini caller'a vermek için.
// 201 Created response'unda `CreatedAtRoute` ile bu Id ile detay endpoint'ine
// pointer veriyoruz.
public sealed record CreateUserCommand(
    string Email,
    string Password,
    string FullName) : IRequest<Result<Guid>>;
