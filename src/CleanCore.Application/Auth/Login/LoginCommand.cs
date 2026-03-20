using CleanCore.Application.Abstractions.Authentication;
using CleanCore.Domain.Shared;
using MediatR;

namespace CleanCore.Application.Auth.Login;

// Email + password ile giriş; başarıda access + refresh token pair döner.
// `Result<AuthResponse>` dönüyor — başarısızlık durumu (yanlış credentials,
// pasif kullanıcı) `UserErrors`'tan Result.Failure olarak handler'dan gelir.
//
// Validator: LoginCommandValidator — sadece "boş mu, email format mı" kontrol eder.
// Şifre uzunluk kuralı login'de YOKTUR — kayıt sırasında kuralı sağlamış olabiliriz,
// sonra kuralı sıkılaştırırsak eski kullanıcılar login olamaz hale gelmesin.
public sealed record LoginCommand(string Email, string Password) : IRequest<Result<AuthResponse>>;
