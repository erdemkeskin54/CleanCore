namespace CleanCore.Domain.Abstractions;

// AggregateRoot: Bir iş sınırının (transaction boundary) dış dünyaya açılan kapısı.
// User aggregate'i UserProfile, UserSettings'leri içerse de dışarıdan sadece User üzerinden erişilir.
// Şimdilik Entity'den fark etmeyen bir marker. Domain event dispatch mekanizması
// gelince (outbox pattern, bkz. docs/ROADMAP.md) event collection + Raise/Clear API buraya döner.
public abstract class AggregateRoot : Entity
{
    protected AggregateRoot(Guid id) : base(id) { }

    protected AggregateRoot() { }
}
