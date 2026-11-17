using InventoryApi.Auth;
using InventoryApi.Data;
using InventoryApi.DTOs;
using InventoryApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InventoryApi.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly JwtTokenService _tokens;
    private readonly JwtOptions _jwtOptions;
    private readonly PasswordHasher<User> _passwordHasher;

    public AuthController(
        AppDbContext db,
        JwtTokenService tokens,
        IOptions<JwtOptions> jwtOptions,
        PasswordHasher<User> passwordHasher)
    {
        _db = db;
        _tokens = tokens;
        _jwtOptions = jwtOptions.Value;
        _passwordHasher = passwordHasher;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var username = request.Username.Trim();
        var exists = await _db.Users.AnyAsync(user => user.Username == username, cancellationToken);
        if (exists)
        {
            return Conflict(new { message = "A user with that username already exists." });
        }

        var user = new User
        {
            Username = username,
            Role = Roles.User,
            CreatedAt = DateTimeOffset.UtcNow
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        return Created("/api/auth/me", ToResponse(user));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await _db.Users.SingleOrDefaultAsync(
            item => item.Username == request.Username.Trim(),
            cancellationToken);

        if (user is null ||
            _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password)
                == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { message = "Invalid username or password." });
        }

        return Ok(ToResponse(user));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<object>> Me(CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId is null || !int.TryParse(userId, out var id))
        {
            return Unauthorized();
        }

        var user = await _db.Users.FindAsync([id], cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(new { user.Id, user.Username, user.Role });
    }

    private AuthResponse ToResponse(User user) => new()
    {
        Token = _tokens.CreateToken(user),
        Username = user.Username,
        Role = user.Role,
        ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(_jwtOptions.ExpirationMinutes)
    };
}
