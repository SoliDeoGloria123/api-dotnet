using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace InventoryApi.Tests;

public class AuthTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public AuthTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_is_public()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/health");
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.Equal("ok", payload?.Status);
    }

    [Fact]
    public async Task Register_login_and_me_return_a_usable_token()
    {
        var client = _factory.CreateClient();
        var username = $"user_{Guid.NewGuid():N}";
        var register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            username,
            password = "Password123"
        });

        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        var registered = await register.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.False(string.IsNullOrWhiteSpace(registered?.Token));
        Assert.Equal(username, registered!.Username);
        Assert.Equal("User", registered.Role);

        var login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            username,
            password = "Password123"
        });
        login.EnsureSuccessStatusCode();
        var loggedIn = await login.Content.ReadFromJsonAsync<AuthResponse>();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loggedIn!.Token);
        var me = await client.GetAsync("/api/auth/me");
        me.EnsureSuccessStatusCode();

        var profile = await me.Content.ReadFromJsonAsync<MeResponse>();
        Assert.Equal(username, profile?.Username);
        Assert.Equal("User", profile?.Role);
    }

    [Fact]
    public async Task Register_rejects_duplicate_usernames_and_short_passwords()
    {
        var client = _factory.CreateClient();
        var username = $"dup_{Guid.NewGuid():N}";
        var first = await client.PostAsJsonAsync("/api/auth/register", new { username, password = "Password123" });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var duplicate = await client.PostAsJsonAsync("/api/auth/register", new { username, password = "Password123" });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var invalid = await client.PostAsJsonAsync("/api/auth/register", new { username = "ab", password = "short" });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    [Fact]
    public async Task Login_rejects_bad_credentials()
    {
        var client = _factory.CreateClient();
        var username = $"login_{Guid.NewGuid():N}";
        var register = await client.PostAsJsonAsync("/api/auth/register", new { username, password = "Password123" });
        register.EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { username, password = "wrong-password" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private sealed record HealthResponse(string Status);
    private sealed record AuthResponse(string Token, string Username, string Role, DateTimeOffset ExpiresAt);
    private sealed record MeResponse(int Id, string Username, string Role);
}
