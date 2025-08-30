using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebAPI.DTOs;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequestDto request)
    {
        // === TODO: Xác thực người dùng với database ===
        // Trong thực tế, bạn sẽ kiểm tra request.Username và request.Password
        // với thông tin user lưu trong database.
        // Ở đây chúng ta giả định đăng nhập thành công.
        if (request.Username != "test" || request.Password != "password")
        {
            return Unauthorized("Invalid credentials");
        }

        // Giả sử user có id và email sau khi xác thực
        var userId = Guid.NewGuid().ToString();
        var userEmail = "test@example.com";

        // === Tạo Token ===
        var issuer = _configuration["JwtSettings:Issuer"];
        var audience = _configuration["JwtSettings:Audience"];
        var key = Encoding.ASCII.GetBytes(_configuration["JwtSettings:Key"]!);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId), // Subject = User ID
                new Claim(JwtRegisteredClaimNames.Email, userEmail),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // JWT ID
            }),
            Expires = DateTime.UtcNow.AddMinutes(
                double.Parse(_configuration["JwtSettings:TokenLifetimeMinutes"]!)),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var jwtToken = tokenHandler.WriteToken(token);

        return Ok(new { Token = jwtToken });
    }
}