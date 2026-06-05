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

        // GET: api/clients
        [HttpGet]
        [Authorize(Roles = "admin,client")]
        public async Task<IActionResult> GetAll()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            IQueryable<Client> query = _db.Clients.Include(c => c.User).AsNoTracking();

            if (role == "client")
                query = query.Where(c => c.UserId == userId);

            var clients = await query.ToListAsync();
            return Ok(clients);
        }

        // GET: api/clients/5
        [HttpGet("{id}")]
        [Authorize(Roles = "admin,client")]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var client = await _db.Clients
                .Include(c => c.User)
                .Include(c => c.Orders)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (client == null) return NotFound();

            if (role == "client" && client.UserId != userId)
                return Forbid();

            return Ok(client);
        }

        // PUT: api/clients/5
        [HttpPut("{id}")]
        [Authorize(Roles = "admin,client")]
        public async Task<IActionResult> Update(int id, [FromBody] ClientUpdateDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var client = await _db.Clients.FindAsync(id);
            if (client == null) return NotFound();

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

        // DELETE: api/clients/5
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

    // DTO для обновления клиента
    public class ClientUpdateDto
    {
        public string? FullName { get; set; }
    }
}

