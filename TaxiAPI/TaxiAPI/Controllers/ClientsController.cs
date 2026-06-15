using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaxiAPI.Data;
using TaxiAPI.Models;

namespace TaxiAPI.Controllers
{
    [ApiController, Route("api/[controller]")]
    [Authorize]
    public class ClientsController : ControllerBase
    {
        private readonly TaxiDbContext _db;

        public ClientsController(TaxiDbContext db) => _db = db;

        // 🔥 НОВОЕ: Получить СВОЙ профиль
        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);

            System.Diagnostics.Debug.WriteLine($"🔐 GetMyProfile: userId={userId}");

            var client = await _db.Clients
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (client == null)
                return NotFound(new { message = "Клиент не найден" });

            return Ok(client);
        }

        // 🔥 НОВОЕ: Обновить СВОЙ профиль
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] ClientUpdateDto dto)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);

            System.Diagnostics.Debug.WriteLine($"🔐 UpdateMyProfile: userId={userId}, fullName={dto.FullName}");

            var client = await _db.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
            if (client == null)
                return NotFound(new { message = "Клиент не найден" });

            if (!string.IsNullOrEmpty(dto.FullName))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(dto.FullName, @"^[a-zA-Zа-яА-ЯёЁ\s\-]+$"))
                    return BadRequest(new { message = "ФИО может содержать только буквы, пробелы и дефисы" });
                client.FullName = dto.FullName;
            }

            client.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(client);
        }

        // GET: api/clients
        [HttpGet]
        [Authorize(Roles = "admin,client")]
        public async Task<IActionResult> GetAll()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            IQueryable<Client> query = _db.Clients.Include(c => c.User).AsNoTracking();

            if (role == "client")
                query = query.Where(c => c.UserId == userId);

            var clients = await query.ToListAsync();
            return Ok(clients);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "admin,client")]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            var client = await _db.Clients
                .Include(c => c.User)
                .Include(c => c.Orders)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (client == null) return NotFound();

            // 🔥 ИСПРАВЛЕНО: сравниваем UserId, а не Id
            if (role == "client" && client.UserId != userId)
                return Forbid();

            return Ok(client);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "admin,client")]
        public async Task<IActionResult> Update(int id, [FromBody] ClientUpdateDto dto)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            var client = await _db.Clients.FindAsync(id);
            if (client == null) return NotFound();

            // 🔥 ИСПРАВЛЕНО: сравниваем UserId
            if (role == "client" && client.UserId != userId)
                return Forbid();

            if (!string.IsNullOrEmpty(dto.FullName))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(dto.FullName, @"^[a-zA-Zа-яА-ЯёЁ\s\-]+$"))
                    return BadRequest(new { message = "ФИО может содержать только буквы, пробелы и дефисы" });
                client.FullName = dto.FullName;
            }

            client.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(client);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var client = await _db.Clients.FindAsync(id);
            if (client == null) return NotFound();

            _db.Clients.Remove(client);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }

    public class ClientUpdateDto
    {
        public string? FullName { get; set; }
    }
}

