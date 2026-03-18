using CleanCore.Application.Abstractions.Authentication;

namespace CleanCore.Infrastructure.Authentication;

// =============================================================================
// BCryptPasswordHasher — BCrypt.Net-Next sarmalayıcı
// =============================================================================
// Niye BCrypt?
//   - Yavaş by design (work factor = 2^N round) → brute force kullanışsız
//   - Salt OTOMATİK üretilip hash içine gömülü → rainbow table imkansız
//   - Endüstri standardı, .NET'te en olgun ve test edilmiş paket
//   - Verify constant-time → timing attack koruması paket içinde
//
// Niye SHA256/SHA512 değil?
//   Hızlılar — saldırgan saniyede milyarlarca deneyebilir. Salt'ı manuel eklesen
//   bile GPU farm'lar wordlist saldırısını hızlı yapar. Password için yanlış araç.
//
// Niye Argon2id veya scrypt değil?
//   Argon2id daha güçlü (memory-hard, GPU'ya dayanıklı) ama .NET'te paketleri
//   olgun değil; production'da BCrypt'tan dönüş yapıldığını gösteren çok proje yok.
//   BCrypt yeter — work factor'ı zamanla 12, 13'e çıkar.
//
// Tradeoff: BCrypt 72-byte password limit'i.
//   72 byte'tan sonra ignore eder. Validator max 128 char ile kapsadık ama
//   72-128 aralığında "fazlası ignore" gerçeği geçerli. Pratikte hiç kimse
//   72+ char password yazmıyor.
// =============================================================================
internal sealed class BCryptPasswordHasher : IPasswordHasher
{
    // Work factor 11 → ~100ms hash maliyeti (modern CPU'da).
    // Niye 11?
    //   - 10: ~50ms (hızlı, hala güvenli ama kalkanı düşürür)
    //   - 11: ~100ms (sweet spot — kullanıcı hissetmez, brute force ezici yavaş)
    //   - 12: ~200ms (login response time'ı artırır, throughput düşer)
    //   - 13: ~400ms (high-security ortamlar — bizim için gereksiz)
    // Donanım hızlandıkça (Moore'un yasası ~2 yılda 1 round'luk artış öneriyor)
    // bu sabit yükseltilebilir. Eski hash'ler çalışmaya devam eder (BCrypt formatı
    // work factor'ı hash içinde tutar — backwards compatible).
    private const int WorkFactor = 11;

    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    // Verify hash içindeki work factor + salt'ı kullanarak yeni hash hesaplar
    // ve constant-time karşılaştırır. Plain password ile DB'deki hash arasında
    // direkt string equality YAPILMAZ — hash format'ı zaten içinde her şeyi tutar.
    public bool Verify(string password, string hash) =>
        BCrypt.Net.BCrypt.Verify(password, hash);
}
