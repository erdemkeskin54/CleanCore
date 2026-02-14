using CleanCore.Domain.Abstractions;

namespace CleanCore.UnitTests.Domain;

public class ValueObjectTests
{
    private sealed class Money : ValueObject
    {
        public decimal Amount { get; }
        public string Currency { get; }

        public Money(decimal amount, string currency)
        {
            Amount = amount;
            Currency = currency;
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Amount;
            yield return Currency;
        }
    }

    [Fact]
    public void Two_value_objects_with_same_components_are_equal()
    {
        var a = new Money(10, "TRY");
        var b = new Money(10, "TRY");

        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Two_value_objects_with_different_components_are_not_equal()
    {
        var a = new Money(10, "TRY");
        var b = new Money(10, "USD");

        Assert.NotEqual(a, b);
        Assert.True(a != b);
    }
}
