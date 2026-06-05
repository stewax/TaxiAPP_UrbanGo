using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TaxiAPI.Data;
using TaxiAPI.DTOs;
using TaxiAPI.Models;
using BCrypt.Net;


namespace TaxiAPI.Controllers
{
    [ApiController, Route("api/[controller]")]
    public class AuthController : Controller
    {
        private readonly TaxiDbContext _db;
        private readonly IConfiguration _config;

        public AuthController(TaxiDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDto dto)
        {
            var user = _db.Users.FirstOrDefault(u => u.Phone == dto.Phone);
            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized(new { message = "Неверный телефон или пароль" });

            var token = GenerateJwtToken(user);
            return Ok(new { token, role = user.Role, userId = user.Id });
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterDto dto)
        {
            if (_db.Users.Any(u => u.Phone == dto.Phone))
                return BadRequest(new { message = "Пользователь с таким телефоном уже существует" });

            var user = new User
            {
                Phone = dto.Phone,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = "client", // 🔥 ЖЕСТКО задаем роль "client"
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            _db.SaveChanges();

            // Создаем профиль клиента
            var client = new Client
            {
                UserId = user.Id,
                FullName = dto.FullName,
                Code = $"CLT-{Guid.NewGuid().ToString()[..6].ToUpper()}",
                CreatedAt = DateTime.UtcNow
            };

            _db.Clients.Add(client);
            _db.SaveChanges();

            return Ok(new { message = "Регистрация успешна", userId = user.Id, role = user.Role });
        }

        private string GenerateJwtToken(User user)
        {
            var key = Encoding.ASCII.GetBytes(_config["Jwt:Key"]!);
            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("phone", user.Phone)
        };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = _config["Jwt:Issuer"],
                Audience = _config["Jwt:Issuer"]
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
