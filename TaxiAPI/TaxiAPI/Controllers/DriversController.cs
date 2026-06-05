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
    public class DriversController : ControllerBase
    {
        private readonly TaxiDbContext _db;

        public DriversController(TaxiDbContext db) => _db = db;

        // GET: api/drivers
        [HttpGet]
        [Authorize(Roles = "admin,driver")]
        public async Task<IActionResult> GetAll()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            IQueryable<Driver> query = _db.Drivers
                .Include(d => d.User)
                .Include(d => d.Car)
                .AsNoTracking();

            if (role == "driver")
                query = query.Where(d => d.UserId == userId);

            var drivers = await query.ToListAsync();
            return Ok(drivers);
        }

        // GET: api/drivers/5
        [HttpGet("{id}")]
        [Authorize(Roles = "admin,driver")]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var driver = await _db.Drivers
                .Include(d => d.User)
                .Include(d => d.Car)
                .Include(d => d.Orders)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (driver == null) return NotFound();

            if (role == "driver" && driver.UserId != userId)
                return Forbid();

            return Ok(driver);
        }

        // PUT: api/drivers/5
        [HttpPut("{id}")]
        [Authorize(Roles = "admin,driver")]
        public async Task<IActionResult> Update(int id, [FromBody] DriverUpdateDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var driver = await _db.Drivers.FindAsync(id);
            if (driver == null) return NotFound();

            if (role == "driver" && driver.UserId != userId)
                return Forbid();

            if (!string.IsNullOrEmpty(dto.FullName))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(dto.FullName, @"^[a-zA-Zа-яА-ЯёЁ\s\-]+$"))
                    return BadRequest(new { message = "ФИО может содержать только буквы, пробелы и дефисы" });
                driver.FullName = dto.FullName;
            }

            if (!string.IsNullOrEmpty(dto.Status))
            {
                if (!new[] { "active", "inactive", "blocked" }.Contains(dto.Status))
                    return BadRequest(new { message = "Неверный статус" });
                driver.Status = dto.Status;
            }

            if (dto.CarId.HasValue)
            {
                var car = await _db.Cars.FindAsync(dto.CarId.Value);
                if (car == null) return BadRequest(new { message = "Автомобиль не найден" });
                driver.CarId = dto.CarId.Value;
            }

            driver.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(driver);
        }

        // GET: api/drivers/5/earnings
        [HttpGet("{id}/earnings")]
        [Authorize(Roles = "admin,driver")]
        public async Task<IActionResult> GetEarnings(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var driver = await _db.Drivers.FindAsync(id);
            if (driver == null) return NotFound();

            if (role == "driver" && driver.UserId != userId)
                return Forbid();

            var completedOrders = await _db.Orders
                .Where(o => o.DriverId == id && o.Status == "completed")
                .ToListAsync();

            var totalEarnings = completedOrders.Sum(o => o.EstimatedCost);
            var totalTrips = completedOrders.Count;

            return Ok(new
            {
                driverId = id,
                totalEarnings,
                totalTrips,
                orders = completedOrders.Select(o => new
                {
                    o.Code,
                    o.EstimatedCost,
                    o.Status,
                    o.CreatedAt
                })
            });
        }

        // DELETE: api/drivers/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var driver = await _db.Drivers.FindAsync(id);
            if (driver == null) return NotFound();

            _db.Drivers.Remove(driver);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }

    // DTO для обновления водителя
    public class DriverUpdateDto
    {
        public string? FullName { get; set; }
        public string? Status { get; set; }
        public int? CarId { get; set; }
    }
}
