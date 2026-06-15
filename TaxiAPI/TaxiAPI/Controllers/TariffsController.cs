using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaxiAPI.Data;
using TaxiAPI.Models;

namespace TaxiAPI.Controllers;

[ApiController, Route("api/[controller]")]
[Authorize]
public class TariffsController : ControllerBase
{
    private readonly TaxiDbContext _db;

    public TariffsController(TaxiDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tariffs = await _db.Tariffs.AsNoTracking().ToListAsync();
        return Ok(tariffs);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var tariff = await _db.Tariffs.FindAsync(id);
        if (tariff == null) return NotFound();
        return Ok(tariff);
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Create([FromBody] TariffDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(new { message = "Название обязательно" });

        var tariff = new Tariff
        {
            Name = dto.Name,
            BasePrice = dto.BasePrice,
            PricePerKm = dto.PricePerKm,
            Code = $"TRF-{Guid.NewGuid().ToString()[..6].ToUpper()}", // Уникальный код
            CreatedAt = DateTime.UtcNow
        };

        _db.Tariffs.Add(tariff);
        await _db.SaveChangesAsync();
        return Ok(tariff);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Update(int id, [FromBody] TariffDto dto)
    {
        var tariff = await _db.Tariffs.FindAsync(id);
        if (tariff == null) return NotFound();

        tariff.Name = dto.Name;
        tariff.BasePrice = dto.BasePrice;
        tariff.PricePerKm = dto.PricePerKm;
        tariff.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(tariff);
    }

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

public class TariffDto
{
    public string Name { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public decimal PricePerKm { get; set; }
}