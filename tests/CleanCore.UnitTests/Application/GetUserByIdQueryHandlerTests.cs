using CleanCore.Application.Users.GetUserById;
using CleanCore.Domain.Users;
using CleanCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CleanCore.UnitTests.Application;

public class GetUserByIdQueryHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public GetUserByIdQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"test-{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task Returns_user_dto_when_found()
    {
        var user = User.Create("mert@example.com", "hash", "Mert");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var handler = new GetUserByIdQueryHandler(_context);

        var result = await handler.Handle(new GetUserByIdQuery(user.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(user.Id, result.Value.Id);
        Assert.Equal("mert@example.com", result.Value.Email);
    }

    [Fact]
    public async Task Returns_not_found_when_user_missing()
    {
        var handler = new GetUserByIdQueryHandler(_context);

        var result = await handler.Handle(new GetUserByIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.NotFound, result.Error);
    }
}
