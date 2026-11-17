using InventoryApi.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace InventoryApi.Tests;

public class ApiFactory : WebApplicationFactory<Program>
{
    public const string TestJwtKey = "TEST-ONLY-KEY-THAT-IS-AT-LEAST-32-CHARS!";

    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"inventory-tests-{Guid.NewGuid():N}.db");

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureHostConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = $"Data Source={_dbPath}",
                ["Jwt:Key"] = TestJwtKey,
                ["Jwt:Issuer"] = "InventoryApi",
                ["Jwt:Audience"] = "InventoryApi",
                ["Jwt:ExpirationMinutes"] = "60"
            });
        });

        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={_dbPath}");
        builder.UseSetting("Jwt:Key", TestJwtKey);
        builder.UseSetting("Jwt:Issuer", "InventoryApi");
        builder.UseSetting("Jwt:Audience", "InventoryApi");
        builder.UseSetting("Jwt:ExpirationMinutes", "60");

        builder.ConfigureServices(services =>
        {
            var optionsDescriptors = services
                .Where(descriptor =>
                    descriptor.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                    descriptor.ServiceType == typeof(DbContextOptions) ||
                    descriptor.ImplementationType == typeof(AppDbContext))
                .ToList();

            foreach (var descriptor in optionsDescriptors)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>(options => options.UseSqlite($"Data Source={_dbPath}"));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        foreach (var path in new[] { _dbPath, _dbPath + "-wal", _dbPath + "-shm" })
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
