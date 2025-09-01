using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using WebAPI.DTOs;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly JwtSettings _jwtSettings;
    public AuthController(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequestDto request)
    {
        if (request.Username != "test" || request.Password != "password")
        {
            return Unauthorized("Invalid credentials");
        }

        var userId = Guid.NewGuid().ToString();
        var userEmail = "test@example.com";

        var issuer = _jwtSettings.Issuer;
        var audience = _jwtSettings.Audience;
        var key = Encoding.ASCII.GetBytes(_jwtSettings.Key!);
        var userTenantId = "acme-corp"; // TODO: Lấy tenantId thực từ người dùng
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(JwtRegisteredClaimNames.Email, userEmail),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("tenantId", userTenantId),
            }),
            Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.TokenLifetimeMinutes),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)

        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var jwtToken = tokenHandler.WriteToken(token);

        return Ok(new { Token = jwtToken });
    }
}