using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaxiAPI.Data;
using TaxiAPI.Models;

namespace TaxiAPI.Controllers
{
    [ApiController, Route("api/[controller]")]
    [Authorize]
    public class CarsController : ControllerBase
    {
        private readonly TaxiDbContext _db;

        public CarsController(TaxiDbContext db) => _db = db;

        // GET: api/cars
        [HttpGet]
        [Authorize(Roles = "admin,driver")]
        public async Task<IActionResult> GetAll()
        {
            var cars = await _db.Cars
                .Include(c => c.Driver)
                .AsNoTracking()
                .ToListAsync();

            return Ok(cars);
        }

        // GET: api/cars/5
        [HttpGet("{id}")]
        [Authorize(Roles = "admin,driver")]
        public async Task<IActionResult> GetById(int id)
        {
            var car = await _db.Cars
                .Include(c => c.Driver)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (car == null) return NotFound();

            return Ok(car);
        }

        // POST: api/cars
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Create([FromBody] CarCreateDto dto)
        {
            if (await _db.Cars.AnyAsync(c => c.LicensePlate == dto.LicensePlate))
                return BadRequest(new { message = "Автомобиль с таким госномером уже существует" });

            var car = new Car
            {
                Brand = dto.Brand,
                Model = dto.Model,
                LicensePlate = dto.LicensePlate,
                ProductionDate = dto.ProductionDate,
                Code = $"CAR-{Guid.NewGuid().ToString()[..6].ToUpper()}",
                DriverId = dto.DriverId,
                CreatedAt = DateTime.UtcNow
            };

            _db.Cars.Add(car);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = car.Id }, car);
        }

        // PUT: api/cars/5
        [HttpPut("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Update(int id, [FromBody] CarUpdateDto dto)
        {
            var car = await _db.Cars.FindAsync(id);
            if (car == null) return NotFound();

            if (!string.IsNullOrEmpty(dto.Brand)) car.Brand = dto.Brand;
            if (!string.IsNullOrEmpty(dto.Model)) car.Model = dto.Model;

            if (!string.IsNullOrEmpty(dto.LicensePlate))
            {
                if (await _db.Cars.AnyAsync(c => c.LicensePlate == dto.LicensePlate && c.Id != id))
                    return BadRequest(new { message = "Автомобиль с таким госномером уже существует" });
                car.LicensePlate = dto.LicensePlate;
            }

            if (dto.ProductionDate.HasValue) car.ProductionDate = dto.ProductionDate.Value;
            if (dto.DriverId.HasValue) car.DriverId = dto.DriverId.Value;

            car.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(car);
        }

        // DELETE: api/cars/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var car = await _db.Cars.FindAsync(id);
            if (car == null) return NotFound();

            _db.Cars.Remove(car);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }

    public class CarCreateDto
    {
        [Required, MaxLength(50)] public string Brand { get; set; } = string.Empty;
        [Required, MaxLength(50)] public string Model { get; set; } = string.Empty;
        [Required, MaxLength(20)] public string LicensePlate { get; set; } = string.Empty;
        [Required] public DateOnly ProductionDate { get; set; }
        public int? DriverId { get; set; }
    }

    public class CarUpdateDto
    {
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public string? LicensePlate { get; set; }
        public DateOnly? ProductionDate { get; set; }
        public int? DriverId { get; set; }
    }
}
