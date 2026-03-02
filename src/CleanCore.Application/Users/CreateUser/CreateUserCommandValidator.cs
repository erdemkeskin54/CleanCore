using FluentValidation;

namespace CleanCore.Application.Users.CreateUser;

// Validator kuralları — kayıt sırasında istediğimiz minimum standart:
//   Email:    boş değil + format sağlam + DB sütun limit'i (256) içinde
//   Password: boş değil + min 8 char (zayıf şifreyi baştan reddet) + max 128 (BCrypt 72-byte limit + buffer)
//   FullName: boş değil + max 200 (DB sütun limit'i)
//
// Niye karmaşık şifre kuralı yok (büyük harf + sayı + sembol)?
//   - Modern öneri (NIST SP 800-63B): uzunluk > karmaşıklık. 8+ char mantıklı.
//   - Pattern dayatmak (ör. "1 büyük + 1 sembol") kullanıcıyı predictable şifrelere
//     yöneltiyor (Password1!, Welcome2024! gibi).
//   - İleride gelirse: zxcvbn gibi entropy-based skor library entegre edilebilir.
//
// Niye max 128?
//   BCrypt 72 byte'tan sonra ignore eder. Kullanıcı 128+ char yazarsa karşılaması zor —
//   kabul edilemez bir UX yerine baştan kapatıyoruz.
public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(128);

        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(200);
    }
}
