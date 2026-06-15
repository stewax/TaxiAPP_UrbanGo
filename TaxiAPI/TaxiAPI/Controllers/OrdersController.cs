using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaxiAPI.Data;
using TaxiAPI.DTOs;
using TaxiAPI.Models;

namespace TaxiAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Требует авторизации для всех методов
public class OrdersController : ControllerBase
{
    private readonly TaxiDbContext _db;

    public OrdersController(TaxiDbContext db)
    {
        _db = db;
    }

    // 🔥 НОВОЕ: Получить доступные заказы для водителей
    [HttpGet("available")]
    [Authorize(Roles = "admin,driver")]
    public async Task<IActionResult> GetAvailableOrders()
    {
        var orders = await _db.Orders
            .Include(o => o.Client)
            .ThenInclude(c => c!.User)
            .Include(o => o.Tariff)
            .Where(o => o.Status == "pending" && o.DriverId == null)
            .OrderBy(o => o.CreatedAt)
            .ToListAsync();

        return Ok(orders.Select(o => new
        {
            o.Id,
            o.Code,
            PickupAddress = o.PickupAddress,
            DestinationAddress = o.DestinationAddress,
            o.EstimatedCost,
            o.Status,
            o.CreatedAt,
            ClientName = o.Client?.FullName ?? "N/A",
            ClientPhone = o.Client?.User?.Phone ?? "N/A",
            TariffName = o.Tariff?.Name ?? "N/A"
        }));
    }

    // 🔥 НОВОЕ: Взять заказ
    [HttpPut("{id}/take")]
    [Authorize(Roles = "admin,driver")]
    public async Task<IActionResult> TakeOrder(int id)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);

        // Находим водителя по UserId
        var driver = await _db.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);
        if (driver == null)
            return NotFound(new { message = "Водитель не найден" });

        var order = await _db.Orders.FindAsync(id);
        if (order == null)
            return NotFound(new { message = "Заказ не найден" });

        if (order.Status != "pending" || order.DriverId != null)
            return BadRequest(new { message = "Заказ уже взят другим водителем" });

        order.DriverId = driver.Id;
        order.Status = "assigned";
        order.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new { message = "Заказ успешно взят", orderId = order.Id });
    }

    // 🔥 НОВОЕ: Начать поездку
    [HttpPut("{id}/start")]
    [Authorize(Roles = "admin,driver")]
    public async Task<IActionResult> StartTrip(int id)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);

        var driver = await _db.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);
        if (driver == null)
            return NotFound(new { message = "Водитель не найден" });

        var order = await _db.Orders.FindAsync(id);
        if (order == null)
            return NotFound(new { message = "Заказ не найден" });

        if (order.DriverId != driver.Id)
            return Forbid();

        if (order.Status != "assigned")
            return BadRequest(new { message = "Заказ уже начат или завершен" });

        order.Status = "in_progress";
        order.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new { message = "Поездка начата", orderId = order.Id });
    }

    // 🔥 НОВОЕ: Завершить поездку
    [HttpPut("{id}/complete")]
    [Authorize(Roles = "admin,driver")]
    public async Task<IActionResult> CompleteTrip(int id, [FromBody] CompleteOrderDto dto)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);

        var driver = await _db.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);
        if (driver == null)
            return NotFound(new { message = "Водитель не найден" });

        var order = await _db.Orders.FindAsync(id);
        if (order == null)
            return NotFound(new { message = "Заказ не найден" });

        if (order.DriverId != driver.Id)
            return Forbid();

        if (order.Status != "in_progress")
            return BadRequest(new { message = "Поездка не начата" });

        order.Status = "completed";
        order.EstimatedCost = dto.FinalCost;
        order.UpdatedAt = DateTime.UtcNow;

        // Создаем запись о поездке
        var trip = new Trip
        {
            OrderId = order.Id,
            DistanceKm = dto.DistanceKm,
            DurationMinutes = dto.DurationMinutes,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            Price = dto.FinalCost,
            Code = $"TRP-{Guid.NewGuid().ToString()[..6].ToUpper()}",
            CreatedAt = DateTime.UtcNow
        };

        _db.Trips.Add(trip);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Поездка завершена", orderId = order.Id, tripId = trip.Id });
    }

    // 🔥 НОВОЕ: Получить активные заказы водителя
    [HttpGet("my-active")]
    [Authorize(Roles = "admin,driver")]
    public async Task<IActionResult> GetMyActiveOrders()
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!);

        var driver = await _db.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);
        if (driver == null)
            return NotFound(new { message = "Водитель не найден" });

        var orders = await _db.Orders
            .Include(o => o.Client)
            .ThenInclude(c => c!.User)
            .Include(o => o.Tariff)
            .Where(o => o.DriverId == driver.Id && (o.Status == "assigned" || o.Status == "in_progress"))
            .ToListAsync();

        return Ok(orders);
    }

    /// <summary>
    /// Создание нового заказа (вызывается из WPF приложения)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "client")]
    public async Task<IActionResult> CreateOrder([FromBody] OrderCreateDto dto)
    {
        // 🔥 ИЗМЕНЕНИЕ: Ищем клиента по UserId (который приходит из WPF), а не по ClientId
        var client = await _db.Clients.FirstOrDefaultAsync(c => c.UserId == dto.ClientId);

        if (client == null)
            return BadRequest(new { message = $"Клиент с UserID={dto.ClientId} не найден. Возможно, профиль не создан." });

        // Проверяем тариф
        var tariff = await _db.Tariffs.FindAsync(dto.TariffId);
        if (tariff == null || tariff.Status != "active")
            return BadRequest(new { message = "Тариф недоступен" });

        // Генерируем код и создаем заказ
        var orderCode = $"ORD-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";

        var order = new Order
        {
            Code = orderCode,
            ClientId = client.Id, // 🔥 ВАЖНО: В БД записываем реальный ID клиента (из таблицы clients)
            TariffId = dto.TariffId,
            PickupAddress = dto.PickupAddress,
            DestinationAddress = dto.DestinationAddress,
            EstimatedCost = dto.EstimatedCost,
            Status = "pending",
            CreatedAt = DateTime.UtcNow
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, order);
    }

    /// <summary>
    /// Получение информации о конкретном заказе
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrderById(int id)
    {
        var order = await _db.Orders
            .Include(o => o.Client)
            .Include(o => o.Driver)
            .Include(o => o.Tariff)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound();

        return Ok(order);
    }

    /// <summary>
    /// Получение списка заказов текущего клиента (для истории поездок)
    /// </summary>
    [HttpGet("my-orders")]
    [Authorize(Roles = "client")]
    public async Task<IActionResult> GetMyOrders([FromQuery] int? userId = null)
    {
        IQueryable<Order> query = _db.Orders
            .Include(o => o.Driver)
            .Include(o => o.Tariff)
            .OrderByDescending(o => o.CreatedAt);

        // 🔥 Фильтруем по userId если передан параметр
        if (userId.HasValue)
        {
            // Находим clientId по userId и фильтруем заказы
            var client = await _db.Clients.FirstOrDefaultAsync(c => c.UserId == userId.Value);
            if (client != null)
            {
                query = query.Where(o => o.ClientId == client.Id);
            }
            else
            {
                return Ok(new List<Order>()); // Клиент не найден — пустой список
            }
        }

        var orders = await query.ToListAsync();
        return Ok(orders);
    }

    /// <summary>
    /// Обновление статуса заказа (например, когда водитель принял заказ)
    /// </summary>
    [HttpPut("{id}/status")]
    [Authorize(Roles = "admin,driver")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] string newStatus)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order == null) return NotFound();

        // Разрешенные статусы
        var allowedStatuses = new[] { "pending", "assigned", "in_progress", "completed", "cancelled" };
        if (!allowedStatuses.Contains(newStatus))
            return BadRequest(new { message = "Недопустимый статус" });

        order.Status = newStatus;
        order.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(order);
    }
    public class CompleteOrderDto
    {
        public decimal FinalCost { get; set; }
        public decimal DistanceKm { get; set; }
        public int DurationMinutes { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}