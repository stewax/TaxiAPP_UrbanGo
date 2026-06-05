using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using TaxiAPI.Data;
using TaxiAPI.Models;

namespace TaxiAPI.Controllers
{
    [ApiController, Route("api/[controller]")]
    [Authorize]
    public class TariffsController : ControllerBase
    {
        private readonly TaxiDbContext _db;

        public TariffsController(TaxiDbContext db) => _db = db;

        // GET: api/tariffs
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var tariffs = await _db.Tariffs
                .Where(t => t.Status == "active")
                .AsNoTracking()
                .ToListAsync();

            return Ok(tariffs);
        }

        // GET: api/tariffs/5
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var tariff = await _db.Tariffs.FindAsync(id);
            if (tariff == null) return NotFound();

            return Ok(tariff);
        }

        // POST: api/tariffs
        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Create([FromBody] TariffCreateDto dto)
        {
            var tariff = new Tariff
            {
                Name = dto.Name,
                BasePrice = dto.BasePrice,
                PricePerKm = dto.PricePerKm,
                MinPrice = dto.MinPrice,
                Status = dto.Status ?? "active",
                Code = $"TRF-{Guid.NewGuid().ToString()[..6].ToUpper()}",
                CreatedAt = DateTime.UtcNow
            };

            _db.Tariffs.Add(tariff);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = tariff.Id }, tariff);
        }

        // PUT: api/tariffs/5
        [HttpPut("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Update(int id, [FromBody] TariffUpdateDto dto)
        {
            var tariff = await _db.Tariffs.FindAsync(id);
            if (tariff == null) return NotFound();

            if (!string.IsNullOrEmpty(dto.Name)) tariff.Name = dto.Name;
            if (dto.BasePrice.HasValue) tariff.BasePrice = dto.BasePrice.Value;
            if (dto.PricePerKm.HasValue) tariff.PricePerKm = dto.PricePerKm.Value;
            if (dto.MinPrice.HasValue) tariff.MinPrice = dto.MinPrice.Value;
            if (!string.IsNullOrEmpty(dto.Status)) tariff.Status = dto.Status;

            tariff.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(tariff);
        }

        // DELETE: api/tariffs/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var tariff = await _db.Tariffs.FindAsync(id);
            if (tariff == null) return NotFound();

            _db.Tariffs.Remove(tariff);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }

    public class TariffCreateDto
    {
        [Required, MaxLength(50)] public string Name { get; set; } = string.Empty;
        [Required, Range(0, 999999.99)] public decimal BasePrice { get; set; }
        [Required, Range(0, 999999.99)] public decimal PricePerKm { get; set; }
        [Required, Range(0, 999999.99)] public decimal MinPrice { get; set; }
        public string? Status { get; set; }
    }

    public class TariffUpdateDto
    {
        public string? Name { get; set; }
        public decimal? BasePrice { get; set; }
        public decimal? PricePerKm { get; set; }
        public decimal? MinPrice { get; set; }
        public string? Status { get; set; }
    }
}
