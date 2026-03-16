using CleanCore.Domain.Shared;

namespace CleanCore.Application.Auth;

// Sadece auth-flow'a özgü hatalar. Kullanıcıyla ilgili hatalar (InvalidCredentials,
// Inactive) Domain tarafında `UserErrors`'ta — tek yerden yönetiliyor.
public static class AuthErrors
{
    public static readonly Error InvalidRefreshToken =
        Error.Unauthorized("Auth.InvalidRefreshToken", "Refresh token geçersiz veya süresi dolmuş.");
}
