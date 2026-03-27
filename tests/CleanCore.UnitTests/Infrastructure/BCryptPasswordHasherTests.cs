using CleanCore.Infrastructure.Authentication;

namespace CleanCore.UnitTests.Infrastructure;

public class BCryptPasswordHasherTests
{
    private readonly BCryptPasswordHasher _hasher = new();

    [Fact]
    public void Hash_returns_non_empty_and_different_from_plain()
    {
        var hash = _hasher.Hash("password123");

        Assert.False(string.IsNullOrEmpty(hash));
        Assert.NotEqual("password123", hash);
    }

    [Fact]
    public void Hash_produces_different_output_each_call_same_input()
    {
        // BCrypt her çağrıda farklı salt üretir.
        var hash1 = _hasher.Hash("password123");
        var hash2 = _hasher.Hash("password123");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Verify_returns_true_for_matching_password()
    {
        var hash = _hasher.Hash("password123");

        Assert.True(_hasher.Verify("password123", hash));
    }

    [Fact]
    public void Verify_returns_false_for_wrong_password()
    {
        var hash = _hasher.Hash("password123");

        Assert.False(_hasher.Verify("wrong-pass", hash));
    }
}
