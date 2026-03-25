using System.Net;
using System.Net.Http.Json;
using CleanCore.Application.Abstractions.Authentication;
using CleanCore.Application.Auth.Login;
using CleanCore.Application.Auth.Refresh;
using CleanCore.Application.Users.CreateUser;

namespace CleanCore.IntegrationTests.Auth;

public class AuthControllerTests : IClassFixture<CleanCoreWebAppFactory>
{
    private readonly HttpClient _client;

    public AuthControllerTests(CleanCoreWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_with_invalid_credentials_returns_401()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginCommand("nobody@example.com", "wrong-pass"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Full_flow_register_login_refresh_logout()
    {
        var email = $"user-{Guid.NewGuid():N}@example.com";
        var password = "password123";

        // 1) Register (POST /users)
        var createResponse = await _client.PostAsJsonAsync("/api/v1/users",
            new CreateUserCommand(email, password, "Flow User"));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        // 2) Login → access + refresh token
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginCommand(email, password));
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var tokens = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(tokens);
        Assert.False(string.IsNullOrEmpty(tokens!.AccessToken));
        Assert.False(string.IsNullOrEmpty(tokens.RefreshToken));

        // 3) Refresh → yeni pair
        var refreshResponse = await _client.PostAsJsonAsync("/api/v1/auth/refresh",
            new RefreshTokenCommand(tokens.RefreshToken));
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

        var newTokens = await refreshResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(newTokens);
        Assert.NotEqual(tokens.RefreshToken, newTokens!.RefreshToken);

        // 4) Eski refresh token artık invalid — rotation doğrulaması
        var reuseResponse = await _client.PostAsJsonAsync("/api/v1/auth/refresh",
            new RefreshTokenCommand(tokens.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, reuseResponse.StatusCode);
    }
}
