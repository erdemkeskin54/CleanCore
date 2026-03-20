using FluentValidation;

namespace CleanCore.Application.Auth.Refresh;

// Refresh validator'ı sadece "boş değil" diyor. Token format/length kontrolü
// burada YAPILMAZ çünkü:
//   - Token'ı biz üretiyoruz (RefreshTokenGenerator) — güvendiğimiz formatta gelir
//   - Saldırgan rastgele bir string yollarsa → handler hash'leyip lookup edecek
//     → bulamayacak → 401 dönecek. Format check'i bilgi sızdırma noktası olabilir.
public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
