using Microsoft.AspNetCore.Mvc;
using MonitoringApi.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System;
using System.Threading.Tasks; // Required for async method

namespace MonitoringApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // e.g., /api/Auth/login
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] AuthRequestDto model)
        {
            // 1. Validate Credentials (Hardcoded for demo, replace with real user store)
            if (model.Username != "admin" || model.Password != "password123") // CHANGE THIS!
            {
                return Unauthorized(new { message = "Invalid credentials" });
            }

            // 2. Generate JWT
            var jwtSettings = _configuration.GetSection("Jwt");
            var secretKey = Encoding.ASCII.GetBytes(jwtSettings["Secret"]);
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];
            var tokenLifetimeMinutes = int.Parse(jwtSettings["TokenLifetimeMinutes"]);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, model.Username), // Subject (username)
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // JWT ID
                // Add other claims like roles if you had them
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(tokenLifetimeMinutes),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(secretKey), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var encodedToken = tokenHandler.WriteToken(token);

            // 3. Return the token
            return Ok(new AuthTokenResponseDto
            {
                AccessToken = encodedToken,
                ExpiresAt = tokenDescriptor.Expires.Value
            });
        }
    }
}