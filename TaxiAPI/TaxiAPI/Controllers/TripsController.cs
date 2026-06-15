using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaxiAPI.Data;

namespace TaxiAPI.Controllers;

[ApiController, Route("api/[controller]")]
[Authorize]
public class TripsController : ControllerBase
{
    private readonly TaxiDbContext _db;

    public TripsController(TaxiDbContext db) => _db = db;

    // 🔥 Получить все поездки текущего водителя
    [HttpGet("driver")]
    [Authorize(Roles = "admin,driver")]
    public async Task<IActionResult> GetDriverTrips()
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);

        var driver = await _db.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);
        if (driver == null)
            return NotFound(new { message = "Водитель не найден" });

        var trips = await _db.Trips
            .Include(t => t.Order)
                .ThenInclude(o => o!.Client)
                    .ThenInclude(c => c!.User)
            .Where(t => t.Order!.DriverId == driver.Id && t.Order.Status == "completed")
            .OrderByDescending(t => t.EndTime)
            .ToListAsync();

        var totalIncome = trips.Sum(t => t.Price);
        var totalDistance = trips.Sum(t => t.DistanceKm);
        var totalTrips = trips.Count;

        return Ok(new
        {
            trips = trips.Select(t => new
            {
                t.Id,
                t.Code,
                t.DistanceKm,
                t.DurationMinutes,
                t.StartTime,
                t.EndTime,
                t.Price,
                ClientName = t.Order?.Client?.FullName ?? "N/A",
                PickupAddress = t.Order?.PickupAddress ?? "N/A",
                DestinationAddress = t.Order?.DestinationAddress ?? "N/A"
            }),
            totalIncome,
            totalDistance,
            totalTrips
        });
    }

    // 🔥 Получить поездки за период
    [HttpGet("driver/period")]
    [Authorize(Roles = "admin,driver")]
    public async Task<IActionResult> GetDriverTripsByPeriod([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);

        var driver = await _db.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);
        if (driver == null)
            return NotFound(new { message = "Водитель не найден" });

        var query = _db.Trips
            .Include(t => t.Order)
                .ThenInclude(o => o!.Client)
                    .ThenInclude(c => c!.User)
            .Where(t => t.Order!.DriverId == driver.Id && t.Order.Status == "completed")
            .AsQueryable();

        if (from.HasValue)
            query = query.Where(t => t.EndTime >= from.Value);
        if (to.HasValue)
            query = query.Where(t => t.EndTime <= to.Value);

        var trips = await query.OrderByDescending(t => t.EndTime).ToListAsync();

        var totalIncome = trips.Sum(t => t.Price);
        var totalDistance = trips.Sum(t => t.DistanceKm);
        var totalTrips = trips.Count;

        return Ok(new
        {
            trips = trips.Select(t => new
            {
                t.Id,
                t.Code,
                t.DistanceKm,
                t.DurationMinutes,
                t.StartTime,
                t.EndTime,
                t.Price,
                ClientName = t.Order?.Client?.FullName ?? "N/A",
                PickupAddress = t.Order?.PickupAddress ?? "N/A",
                DestinationAddress = t.Order?.DestinationAddress ?? "N/A"
            }),
            totalIncome,
            totalDistance,
            totalTrips
        });
    }
    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetAllTrips()
    {
        var trips = await _db.Trips
            .Include(t => t.Order)
                .ThenInclude(o => o!.Client)
                    .ThenInclude(c => c!.User)
            .Include(t => t.Order)
                .ThenInclude(o => o!.Driver)
                    .ThenInclude(d => d!.User)
            .OrderByDescending(t => t.EndTime)
            .ToListAsync();

        var totalIncome = trips.Sum(t => t.Price);
        var totalTrips = trips.Count;

        return Ok(new
        {
            trips = trips.Select(t => new
            {
                t.Id,
                t.Code,
                t.DistanceKm,
                t.DurationMinutes,
                t.StartTime,
                t.EndTime,
                t.Price,
                ClientName = t.Order?.Client?.FullName ?? "N/A",
                DriverName = t.Order?.Driver?.FullName ?? "N/A"
            }),
            totalIncome,
            totalTrips
        });
    }
}