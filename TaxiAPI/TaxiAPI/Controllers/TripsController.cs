using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TaxiAPI.Data;
using TaxiAPI.Models;

namespace TaxiAPI.Controllers
{
    [ApiController, Route("api/[controller]")]
    [Authorize]
    public class TripsController : ControllerBase
    {
        private readonly TaxiDbContext _db;

        public TripsController(TaxiDbContext db) => _db = db;

        // GET: api/trips
        [HttpGet]
        [Authorize(Roles = "admin,driver,client")]
        public async Task<IActionResult> GetAll()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            IQueryable<Trip> query = _db.Trips
                .Include(t => t.Order)
                .ThenInclude(o => o!.Driver)
                .Include(t => t.Order)
                .ThenInclude(o => o!.Client)
                .AsNoTracking();

            if (role == "driver")
            {
                query = query.Where(t => t.Order!.DriverId == userId);
            }
            else if (role == "client")
            {
                query = query.Where(t => t.Order!.ClientId == userId);
            }

            var trips = await query.OrderByDescending(t => t.StartTime).ToListAsync();
            return Ok(trips);
        }

        // GET: api/trips/5
        [HttpGet("{id}")]
        [Authorize(Roles = "admin,driver,client")]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var trip = await _db.Trips
                .Include(t => t.Order)
                .ThenInclude(o => o!.Driver)
                .Include(t => t.Order)
                .ThenInclude(o => o!.Client)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (trip == null) return NotFound();

            if (role == "driver" && trip.Order!.DriverId != userId)
                return Forbid();

            if (role == "client" && trip.Order.ClientId != userId)
                return Forbid();

            return Ok(trip);
        }

        // POST: api/trips
        [HttpPost]
        [Authorize(Roles = "admin,driver")]
        public async Task<IActionResult> Create([FromBody] TripCreateDto dto)
        {
            var order = await _db.Orders.FindAsync(dto.OrderId);
            if (order == null) return NotFound(new { message = "Заказ не найден" });

            if (order.Status != "completed")
                return BadRequest(new { message = "Нельзя создать поездку для незавершённого заказа" });

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            if (order.DriverId != userId && User.FindFirst(ClaimTypes.Role)?.Value == "driver")
                return Forbid();

            if (dto.EndTime <= dto.StartTime)
                return BadRequest(new { message = "Время завершения должно быть позже времени начала" });

            var trip = new Trip
            {
                OrderId = dto.OrderId,
                DistanceKm = dto.DistanceKm,
                DurationMinutes = dto.DurationMinutes,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                Price = dto.Price,
                Code = $"TRP-{Guid.NewGuid().ToString()[..6].ToUpper()}",
                CreatedAt = DateTime.UtcNow
            };

            _db.Trips.Add(trip);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = trip.Id }, trip);
        }

        // PUT: api/trips/5
        [HttpPut("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Update(int id, [FromBody] TripUpdateDto dto)
        {
            var trip = await _db.Trips.FindAsync(id);
            if (trip == null) return NotFound();

            if (dto.DistanceKm.HasValue) trip.DistanceKm = dto.DistanceKm.Value;
            if (dto.DurationMinutes.HasValue) trip.DurationMinutes = dto.DurationMinutes.Value;
            if (dto.StartTime.HasValue) trip.StartTime = dto.StartTime.Value;
            if (dto.EndTime.HasValue)
            {
                if (dto.EndTime <= trip.StartTime)
                    return BadRequest(new { message = "Время завершения должно быть позже времени начала" });
                trip.EndTime = dto.EndTime.Value;
            }
            if (dto.Price.HasValue) trip.Price = dto.Price.Value;

            trip.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(trip);
        }

        // DELETE: api/trips/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var trip = await _db.Trips.FindAsync(id);
            if (trip == null) return NotFound();

            _db.Trips.Remove(trip);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }

    public class TripCreateDto
    {
        [Required] public int OrderId { get; set; }
        [Required, Range(0, 99999.99)] public decimal DistanceKm { get; set; }
        [Required, Range(0, int.MaxValue)] public int DurationMinutes { get; set; }
        [Required] public DateTime StartTime { get; set; }
        [Required] public DateTime EndTime { get; set; }
        [Required, Range(0, 999999.99)] public decimal Price { get; set; }
    }

    public class TripUpdateDto
    {
        public decimal? DistanceKm { get; set; }
        public int? DurationMinutes { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public decimal? Price { get; set; }
    }
}
