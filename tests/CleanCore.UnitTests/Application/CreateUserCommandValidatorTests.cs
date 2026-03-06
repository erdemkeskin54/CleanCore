using CleanCore.Application.Users.CreateUser;

namespace CleanCore.UnitTests.Application;

public class CreateUserCommandValidatorTests
{
    private readonly CreateUserCommandValidator _validator = new();

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Invalid_email_fails_validation(string email)
    {
        var command = new CreateUserCommand(email, "password123", "Mert");

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateUserCommand.Email));
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    public void Short_or_empty_password_fails(string password)
    {
        var command = new CreateUserCommand("mert@example.com", password, "Mert");

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateUserCommand.Password));
    }

    [Fact]
    public void Empty_fullname_fails()
    {
        var command = new CreateUserCommand("mert@example.com", "password123", "");

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateUserCommand.FullName));
    }

    [Fact]
    public void Valid_command_passes()
    {
        var command = new CreateUserCommand("mert@example.com", "securepass123", "Mert İğdir");

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }
}
