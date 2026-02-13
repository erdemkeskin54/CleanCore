using CleanCore.Domain.Shared;

namespace CleanCore.Domain.Users;

// Hata kodları tek yerde — handler'lar bunları referans eder, magic string yok.
public static class UserErrors
{
    public static readonly Error NotFound =
        Error.NotFound("User.NotFound", "Kullanıcı bulunamadı.");

    public static readonly Error EmailAlreadyExists =
        Error.Conflict("User.EmailAlreadyExists", "Bu email ile kayıtlı bir kullanıcı zaten var.");

    public static readonly Error InvalidCredentials =
        Error.Unauthorized("User.InvalidCredentials", "Email veya şifre hatalı.");

    public static readonly Error Inactive =
        Error.Forbidden("User.Inactive", "Kullanıcı pasif durumda.");
}
