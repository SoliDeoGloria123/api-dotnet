using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace InventoryApi.Tests;

public class ProductTests : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ApiFactory _factory;

    public ProductTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Products_require_a_bearer_token()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/products");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Authenticated_user_can_create_list_update_adjust_and_delete_a_product()
    {
        var client = await AuthenticateAsync();
        var sku = $"SKU-{Guid.NewGuid():N}"[..20].ToUpperInvariant();

        var create = await client.PostAsJsonAsync("/api/products", new
        {
            name = "Wireless mouse",
            sku,
            description = "USB wireless mouse",
            category = "Accessories",
            quantity = 10,
            unitPrice = 89.90
        });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<ProductResponse>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal("Wireless mouse", created!.Name);
        Assert.Equal(sku, created.Sku);
        Assert.Equal(10, created.Quantity);

        var get = await client.GetAsync($"/api/products/{created.Id}");
        get.EnsureSuccessStatusCode();

        var list = await client.GetAsync($"/api/products?search={sku}&page=1&pageSize=10");
        list.EnsureSuccessStatusCode();
        var page = await list.Content.ReadFromJsonAsync<PagedResponse<ProductResponse>>(JsonOptions);
        Assert.Equal(1, page?.TotalCount);
        Assert.Equal(sku, page!.Items[0].Sku);

        var update = await client.PutAsJsonAsync($"/api/products/{created.Id}", new
        {
            name = "Wireless mouse Pro",
            description = "Updated",
            category = "Accessories",
            unitPrice = 99.50
        });
        update.EnsureSuccessStatusCode();
        var updated = await update.Content.ReadFromJsonAsync<ProductResponse>(JsonOptions);
        Assert.Equal("Wireless mouse Pro", updated?.Name);
        Assert.Equal(99.50m, updated?.UnitPrice);

        var stock = await client.PatchAsJsonAsync($"/api/products/{created.Id}/stock", new { delta = -3 });
        stock.EnsureSuccessStatusCode();
        var adjusted = await stock.Content.ReadFromJsonAsync<ProductResponse>(JsonOptions);
        Assert.Equal(7, adjusted?.Quantity);

        var delete = await client.DeleteAsync($"/api/products/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var missing = await client.GetAsync($"/api/products/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task Create_rejects_duplicate_sku_and_invalid_payloads()
    {
        var client = await AuthenticateAsync();
        var sku = $"DUP-{Guid.NewGuid():N}"[..20].ToUpperInvariant();

        var first = await client.PostAsJsonAsync("/api/products", new
        {
            name = "Keyboard",
            sku,
            quantity = 1,
            unitPrice = 10
        });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var duplicate = await client.PostAsJsonAsync("/api/products", new
        {
            name = "Another keyboard",
            sku,
            quantity = 1,
            unitPrice = 10
        });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var invalid = await client.PostAsJsonAsync("/api/products", new
        {
            name = "",
            sku = "bad sku",
            quantity = -1,
            unitPrice = -5
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    [Fact]
    public async Task Stock_adjustment_cannot_go_below_zero()
    {
        var client = await AuthenticateAsync();
        var sku = $"STK-{Guid.NewGuid():N}"[..20].ToUpperInvariant();
        var create = await client.PostAsJsonAsync("/api/products", new
        {
            name = "Cable",
            sku,
            quantity = 2,
            unitPrice = 5
        });
        var product = await create.Content.ReadFromJsonAsync<ProductResponse>(JsonOptions);

        var response = await client.PatchAsJsonAsync($"/api/products/{product!.Id}/stock", new { delta = -5 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<HttpClient> AuthenticateAsync()
    {
        var client = _factory.CreateClient();
        var username = $"prod_{Guid.NewGuid():N}";
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            username,
            password = "Password123"
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }

    private sealed record AuthResponse(string Token, string Username, string Role, DateTimeOffset ExpiresAt);

    private sealed record ProductResponse(
        int Id,
        string Name,
        string Sku,
        string? Description,
        string? Category,
        int Quantity,
        decimal UnitPrice,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);

    private sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
}
