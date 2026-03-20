using FluentValidation;

namespace CleanCore.Application.Auth.Login;

// Login validator'ı **bilinçli olarak az kuralcı**:
//   - Email: boş olmasın + email formatı (yapısal sanity check)
//   - Password: boş olmasın — uzunluk/karmaşıklık kuralı YOK
//
// Niye password kuralı yok?
//   Kayıt sırasında (CreateUserCommandValidator) kuralı koyduk: min 8 char.
//   İleride kuralı sıkılaştırırsak (ör. 12 char) eski kullanıcılar login
//   ekranında 400 alır — ama yapması gereken şey login olup şifresini değiştirmek.
//   Login validator'da kuralı koymak = eski kullanıcıyı dışarıda kilitlemek.
public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}
