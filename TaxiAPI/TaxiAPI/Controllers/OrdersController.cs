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
        System.Diagnostics.Debug.WriteLine("========== COMPLETE TRIP ==========");
        System.Diagnostics.Debug.WriteLine($"Получен FinalCost: {dto.FinalCost}");
        System.Diagnostics.Debug.WriteLine($"Тип FinalCost: {dto.FinalCost.GetType()}");
        System.Diagnostics.Debug.WriteLine($"FinalCost * 100: {dto.FinalCost * 100}");
        System.Diagnostics.Debug.WriteLine($"FinalCost / 100: {dto.FinalCost / 100}");

        // Проверяем, не пришли ли копейки
        if (dto.FinalCost > 10000)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ FinalCost={dto.FinalCost} - похоже на КОПЕЙКИ!");
            System.Diagnostics.Debug.WriteLine($"💰 В рублях: {dto.FinalCost / 100}");
        }

        // Создаем Trip
        var trip = new Trip
        {
            OrderId = order.Id,
            DistanceKm = dto.DistanceKm,
            DurationMinutes = dto.DurationMinutes,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            Price = dto.FinalCost, // ← ПРОВЕРЬТЕ ЭТО МЕСТО
            Code = $"TRP-{Guid.NewGuid().ToString()[..6].ToUpper()}",
            CreatedAt = DateTime.UtcNow
        };

        System.Diagnostics.Debug.WriteLine($"Сохраняем Price: {trip.Price}");

        _db.Trips.Add(trip);
        await _db.SaveChangesAsync();

        // Проверяем, что сохранилось
        var savedTrip = await _db.Trips.AsNoTracking().FirstOrDefaultAsync(t => t.Id == trip.Id);
        System.Diagnostics.Debug.WriteLine($"Сохраненный Price: {savedTrip?.Price}");

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
        System.Diagnostics.Debug.WriteLine("========== СЕРВЕР: CREATE ORDER ==========");
        System.Diagnostics.Debug.WriteLine($"Получен dto.EstimatedCost: {dto.EstimatedCost}");
        System.Diagnostics.Debug.WriteLine($"Тип: {dto.EstimatedCost.GetType()}");

        // Проверяем сырой JSON (если доступен)
        var rawJson = await Request.BodyReader.ReadAsync();
        System.Diagnostics.Debug.WriteLine($"Сырой JSON: {rawJson}");
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

    // 🔥 Назначение водителя на заказ (для админа)
    [HttpPut("{id}/assign-driver")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> AssignDriver(int id, [FromBody] AssignDriverDto dto)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order == null)
            return NotFound(new { message = "Заказ не найден" });

        if (order.Status != "pending")
            return BadRequest(new { message = "Можно назначить водителя только на заказ со статусом 'pending'" });

        var driver = await _db.Drivers.FindAsync(dto.DriverId);
        if (driver == null)
            return NotFound(new { message = "Водитель не найден" });

        if (driver.Status != "active")
            return BadRequest(new { message = "Водитель не активен" });

        order.DriverId = driver.Id;
        order.Status = "assigned";
        order.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new { message = "Водитель назначен", orderId = order.Id, driverId = driver.Id });
    }

    // 🔥 Получить список активных водителей (для админа)
    [HttpGet("available-drivers")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetAvailableDrivers()
    {
        var drivers = await _db.Drivers
            .Include(d => d.User)
            .Where(d => d.Status == "active")
            .Select(d => new
            {
                d.Id,
                FullName = d.FullName,
                Phone = d.User != null ? d.User.Phone : "N/A",
                d.Code,
                d.Status
            })
            .ToListAsync();

        return Ok(drivers);
    }

    public class AssignDriverDto
    {
        public int DriverId { get; set; }
    }

    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[API] Запрос всех заказов...");

            var orders = await _db.Orders
                .Include(o => o.Client)
                    .ThenInclude(c => c!.User)
                .Include(o => o.Driver)
                    .ThenInclude(d => d!.User)
                .Include(o => o.Tariff)
                .Select(o => new
                {
                    o.Id,
                    o.Code,
                    o.ClientId,
                    ClientName = o.Client != null ? o.Client.FullName : "N/A",
                    o.DriverId,
                    DriverName = o.Driver != null ? o.Driver.FullName : null,
                    o.PickupAddress,
                    o.DestinationAddress,
                    o.Status,
                    o.EstimatedCost,
                    o.CreatedAt
                })
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            System.Diagnostics.Debug.WriteLine($"[API] Найдено {orders.Count} заказов");
            return Ok(orders);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[API] Ошибка: {ex.Message}");
            return StatusCode(500, new { message = $"Ошибка сервера: {ex.Message}" });
        }
    }

    // 🔥 Изменение статуса заказа (для админа)
    [HttpPut("{id}/status")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> ChangeStatus(int id, [FromBody] ChangeStatusDto dto)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order == null)
            return NotFound(new { message = "Заказ не найден" });

        var allowedStatuses = new[] { "pending", "assigned", "in_progress", "completed", "cancelled" };
        if (!allowedStatuses.Contains(dto.Status))
            return BadRequest(new { message = "Недопустимый статус" });

        order.Status = dto.Status;
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Статус изменен", newStatus = order.Status });
    }

    // 🔥 Удаление заказа (для админа)
    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order == null)
            return NotFound(new { message = "Заказ не найден" });

        _db.Orders.Remove(order);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Заказ удален" });
    }

    public class ChangeStatusDto
    {
        public string Status { get; set; } = string.Empty;
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