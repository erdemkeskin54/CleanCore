namespace CleanCore.Application.Abstractions.Authentication;

// Password hashing ve doğrulama soyutlaması.
// Implementation: `Infrastructure/Authentication/BCryptPasswordHasher` (work factor 11).
//
// Neden ayrı interface?
//   - Application katmanı BCrypt paketine direkt bağımlı olmasın — algoritma
//     değişirse (Argon2id, scrypt vs) tek nokta etkilensin.
//   - Test'te FakeHasher (sadece string equality) kullanmak için seam.
//
// Verify'da sabit zamanlı (constant-time) karşılaştırma BCrypt.Net.BCrypt.Verify
// içinde zaten var — timing attack'lara karşı manuel önlem almıyoruz, paket halletmiş.
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
