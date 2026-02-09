namespace CleanCore.Domain.Abstractions;

// =============================================================================
// Entity — DDD building block
// =============================================================================
// Id'si olan ve **kimliği üzerinden** eşitlenen her şeyin base class'ı.
// İki User aynı email/isim/rol'e sahip olsa bile **farklı entity**'dir — Id farklıysa farklıdır.
//
// ValueObject farkı: ValueObject'in kimliği değerleriyse, Entity'nin kimliği Id'si.
//   User(Id=1, Email="a@b.com") ≠ User(Id=2, Email="a@b.com") — farklı kullanıcı
//   Email("a@b.com") == Email("a@b.com") — aynı değer
//
// Id tipi: Guid
//   - DB generate değil, domain'de oluşturuyoruz (Guid.NewGuid). Test'te mock'lamak kolay.
//   - Int/long identity alternative: DB round-trip'i gerekir. Microservice'lerde de sıkıntı.
//   - Guid uniqueness global — farklı tablolardaki id çakışmaz, log'da karışıklık olmaz.
// =============================================================================
public abstract class Entity : IEquatable<Entity>
{
    protected Entity(Guid id)
    {
        Id = id;
    }

    // EF Core reflection ile entity hydrate'lerken parametresiz ctor'ı kullanır.
    protected Entity() { }

    // protected set: dışarıdan atanamaz, ama EF Core + türetilmiş class'ın factory'si yazabilir.
    public Guid Id { get; protected set; }

    public bool Equals(Entity? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        // GetType() check'i: Inheritance'ta proxy sınıflar (EF Core lazy loading)
        // base type ile aynı Id'ye sahip olabiliyor — farklı tip, farklı entity say.
        return GetType() == other.GetType() && Id == other.Id;
    }

    public override bool Equals(object? obj) => obj is Entity other && Equals(other);

    // HashCode tipten ve Id'den türer. HashSet/Dictionary key'i olarak kullanılabilir.
    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity? left, Entity? right) => Equals(left, right);

    public static bool operator !=(Entity? left, Entity? right) => !Equals(left, right);
}
