namespace InventoryApi.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = Roles.User;
    public DateTimeOffset CreatedAt { get; set; }
}

public static class Roles
{
    public const string User = "User";
    public const string Admin = "Admin";
}
