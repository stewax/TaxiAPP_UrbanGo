using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TaxiAPI.Data;
using TaxiAPI.DTOs;
using TaxiAPI.Models;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;

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
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Phone == dto.Phone);
            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized(new { message = "Неверный телефон или пароль" });

            // 🔥 Если пользователь driver, но нет профиля - создаем автоматически
            if (user.Role == "driver")
            {
                var driverExists = await _db.Drivers.AnyAsync(d => d.UserId == user.Id);
                if (!driverExists)
                {
                    // Пробуем взять имя из Client, если есть
                    var client = await _db.Clients.FirstOrDefaultAsync(c => c.UserId == user.Id);
                    var fullName = client?.FullName ?? $"Водитель {user.Phone}";

                    var driver = new Driver
                    {
                        UserId = user.Id,
                        FullName = fullName,
                        Status = "active",
                        Code = $"DRV-{Guid.NewGuid().ToString()[..6].ToUpper()}",
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.Drivers.Add(driver);
                    await _db.SaveChangesAsync();
                }
            }

            // 🔥 ИСПРАВЛЕНО: используем существующий метод GenerateJwtToken
            var token = GenerateJwtToken(user);

            return Ok(new
            {
                token,
                role = user.Role,
                userId = user.Id,
                message = "Вход выполнен успешно"
            });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            // 1. Проверяем, не занят ли телефон
            if (await _db.Users.AnyAsync(u => u.Phone == dto.Phone))
                return BadRequest(new { message = "Пользователь с таким телефоном уже существует" });

            // 2. Создаем пользователя в таблице users
            var user = new User
            {
                Phone = dto.Phone,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = "client", // 🔥 Жестко задаем роль client
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync(); // Сохраняем, чтобы получить user.Id

            // 3. АВТОМАТИЧЕСКИ создаем профиль клиента
            var client = new Client
            {
                UserId = user.Id, // Связываем с созданным пользователем
                FullName = dto.FullName,
                Code = $"CLT-{Guid.NewGuid().ToString()[..6].ToUpper()}", // Уникальный код
                CreatedAt = DateTime.UtcNow
            };

            _db.Clients.Add(client);
            await _db.SaveChangesAsync(); // Сохраняем клиента

            return Ok(new
            {
                message = "Регистрация успешна",
                userId = user.Id,
                clientId = client.Id,
                role = user.Role
            });
        }

        // 🔥 Существующий метод генерации JWT токена
        private string GenerateJwtToken(User user)
        {
            var key = Encoding.ASCII.GetBytes(_config["Jwt:Key"]!);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("phone", user.Phone),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature),
                Issuer = _config["Jwt:Issuer"],
                Audience = _config["Jwt:Issuer"]
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}