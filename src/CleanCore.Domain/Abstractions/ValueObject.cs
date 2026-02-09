namespace CleanCore.Domain.Abstractions;

// =============================================================================
// ValueObject — DDD building block
// =============================================================================
// Id'si olmayan, değerleri aynıysa eşit olan küçük domain tipi.
// Örnek: Email("a@b.com"), Money(100, "TRY"), Address("İstanbul", "Kadıköy").
// İki Email("x@y.com") birbirine eşittir — farklı Id'leri yoktur, değerleri aynıdır.
//
// Entity farkı: Entity'nin kimliği Id'si, ValueObject'in kimliği içindeki değerleridir.
//
// Pattern: Template Method
//   Equals/GetHashCode algoritması burada sabit; değerlerin ne olduğunu türetilmiş
//   class kendisi söylüyor (GetEqualityComponents). Böylece her ValueObject doğru
//   davranışı otomatik kazanıyor, sadece hangi alanların sayılacağını belirtiyor.
//
// Örnek implementasyon:
//   public sealed class Email : ValueObject
//   {
//       public string Value { get; }
//       public Email(string value) => Value = value.ToLowerInvariant();
//       protected override IEnumerable<object?> GetEqualityComponents()
//       {
//           yield return Value;
//       }
//   }
// =============================================================================
public abstract class ValueObject : IEquatable<ValueObject>
{
    // Hangi alanlar karşılaştırmaya girer? Türetilmiş class söyler.
    // yield return ile liste döndürmek en pratik yol.
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public bool Equals(ValueObject? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        // GetType() check'i: Money("100", "TRY") ile Discount("100", "TRY") eşit olmasın
        // (ikisi de aynı componentleri döndürebilir ama farklı tiplerdir).
        return GetType() == other.GetType()
            && GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override bool Equals(object? obj) => obj is ValueObject other && Equals(other);

    // HashSet/Dictionary key'i olarak kullanılabilir olması için GetHashCode override.
    // Component'lerin hash'lerini birleştiriyoruz; null component varsa 0 sayıyoruz.
    public override int GetHashCode() =>
        GetEqualityComponents()
            .Aggregate(0, (hash, component) => HashCode.Combine(hash, component?.GetHashCode() ?? 0));

    public static bool operator ==(ValueObject? left, ValueObject? right) => Equals(left, right);

    public static bool operator !=(ValueObject? left, ValueObject? right) => !Equals(left, right);
}
