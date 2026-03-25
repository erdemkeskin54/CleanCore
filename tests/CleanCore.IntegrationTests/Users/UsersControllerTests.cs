using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CleanCore.Application.Abstractions.Authentication;
using CleanCore.Application.Auth.Login;
using CleanCore.Application.Users.CreateUser;
using CleanCore.Application.Users.GetUserById;

namespace CleanCore.IntegrationTests.Users;

public class UsersControllerTests : IClassFixture<CleanCoreWebAppFactory>
{
    private readonly HttpClient _client;

    public UsersControllerTests(CleanCoreWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_invalid_user_returns_400()
    {
        var command = new CreateUserCommand(Email: "", Password: "short", FullName: "");

        var response = await _client.PostAsJsonAsync("/api/v1/users", command);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_user_without_token_returns_401()
    {
        var response = await _client.GetAsync($"/api/v1/users/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_then_login_then_get_returns_dto()
    {
        var email = $"get-{Guid.NewGuid():N}@example.com";
        var password = "password123";

        // Create
        var create = await _client.PostAsJsonAsync("/api/v1/users",
            new CreateUserCommand(email, password, "Get Test User"));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<IdResponse>();
        Assert.NotNull(created);

        // Login to get access token
        var login = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginCommand(email, password));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var tokens = await login.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(tokens);

        // Authenticated GET
        using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/users/{created!.Id}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
        var getResponse = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var dto = await getResponse.Content.ReadFromJsonAsync<UserDto>();
        Assert.NotNull(dto);
        Assert.Equal(email, dto!.Email);
    }

    private sealed record IdResponse(Guid Id);
}
