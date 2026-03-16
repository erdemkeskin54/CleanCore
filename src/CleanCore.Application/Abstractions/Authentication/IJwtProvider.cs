using CleanCore.Domain.Users;

namespace CleanCore.Application.Abstractions.Authentication;

// JWT access token üreten servisin sözleşmesi.
// Implementation Infrastructure'da (HmacSha256 ile imzalı). Domain ya da
// Application katmanı System.IdentityModel.* paketlerine bağımlı olmasın diye
// abstraction Application'da tutuluyor.
//
// Tuple döndürmesinin sebebi: handler hem token string'ine (response'a koymak için)
// hem de expiry'ye (response'a koymak + DB'ye yazmak için) ihtiyaç duyar. İkisini
// ayrı method ile dönmek redundant — zaten her ikisi de aynı `now` baz alınarak
// hesaplanıyor.
public interface IJwtProvider
{
    (string Token, DateTime ExpiresAt) GenerateAccessToken(User user);
}
