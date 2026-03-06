using CleanCore.Application.Abstractions.Authentication;
using CleanCore.Application.Users.CreateUser;
using CleanCore.Domain.Users;
using CleanCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CleanCore.UnitTests.Application;

public class CreateUserCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _hasher = new FakePasswordHasher();

    public CreateUserCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"test-{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task Creates_user_and_returns_id()
    {
        var handler = new CreateUserCommandHandler(_context, _hasher);
        var command = new CreateUserCommand("mert@example.com", "password123", "Mert");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);

        var saved = await _context.Users.FirstOrDefaultAsync(u => u.Id == result.Value);
        Assert.NotNull(saved);
        Assert.Equal("mert@example.com", saved!.Email);
        Assert.Equal("Mert", saved.FullName);
        Assert.NotEqual("password123", saved.PasswordHash);
    }

    [Fact]
    public async Task Returns_conflict_when_email_already_exists()
    {
        var existing = User.Create("mert@example.com", "hash", "Mert");
        _context.Users.Add(existing);
        await _context.SaveChangesAsync();

        var handler = new CreateUserCommandHandler(_context, _hasher);
        var command = new CreateUserCommand("MERT@example.com", "password123", "Different");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.EmailAlreadyExists, result.Error);
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => $"hashed:{password}";
        public bool Verify(string password, string hash) => hash == $"hashed:{password}";
    }
}
