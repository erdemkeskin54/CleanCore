using CleanCore.Domain.Shared;

namespace CleanCore.UnitTests.Domain;

public class ResultTests
{
    [Fact]
    public void Success_returns_successful_result_with_no_error()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Failure_with_error_returns_failed_result()
    {
        var error = Error.Validation("User.Email", "Email geçersiz.");

        var result = Result.Failure(error);

        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void Generic_success_wraps_value()
    {
        var result = Result.Success("ok");

        Assert.True(result.IsSuccess);
        Assert.Equal("ok", result.Value);
    }

    [Fact]
    public void Generic_failure_accessing_value_throws()
    {
        var result = Result.Failure<string>(Error.NotFound("User.NotFound", "kullanıcı yok"));

        Assert.Throws<InvalidOperationException>(() => _ = result.Value);
    }

    [Fact]
    public void Implicit_cast_from_value_creates_success()
    {
        Result<int> result = 42;

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Implicit_cast_from_error_creates_failure()
    {
        Result<int> result = Error.NotFound("X.NotFound", "bulunamadı");

        Assert.True(result.IsFailure);
    }
}
