using FluentValidation;

namespace CleanCore.Application.Auth.Logout;

// Sadece "boş mu" kontrolü. Token formatını burada değil — handler'da hash
// lookup'ı zaten "geçerli mi" sorusuna cevap veriyor.
public sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
