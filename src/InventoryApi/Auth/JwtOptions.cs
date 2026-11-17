namespace InventoryApi.Auth;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// Development-only default. Override with the Jwt__Key environment variable
    /// or a secret store before deploying anywhere public.
    /// </summary>
    public const string DevelopmentKey = "DEV-ONLY-DO-NOT-USE-IN-PRODUCTION-CHANGE-ME-32+";

    public string Key { get; set; } = DevelopmentKey;
    public string Issuer { get; set; } = "InventoryApi";
    public string Audience { get; set; } = "InventoryApi";
    public int ExpirationMinutes { get; set; } = 60;
}
