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
public class UsersController : ControllerBase
{
    private readonly TaxiDbContext _db;

    public UsersController(TaxiDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Получение профиля текущего пользователя (данные из tables users + clients)
    /// URL: GET /api/users/me
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        // Получаем ID из токена
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int currentUserId))
            return Unauthorized(new { message = "Некорректный токен" });

        // Ищем пользователя и джойним клиента
        // Используем Left Join через GroupJoin/SelectMany или просто проверяем наличие
        var user = await _db.Users.FindAsync(currentUserId);

        if (user == null)
        {
            System.Diagnostics.Debug.WriteLine($"[Server] Пользователь с ID {currentUserId} не найден в Users");
            return NotFound(new { message = "Пользователь не найден" });
        }

        // Ищем профиль клиента
        var client = await _db.Clients.FirstOrDefaultAsync(c => c.UserId == currentUserId);

        if (client == null)
        {
            System.Diagnostics.Debug.WriteLine($"[Server] Клиент для UserId {currentUserId} не найден в Clients");
            // Можно вернуть данные пользователя без клиента, или ошибку
            // Для профиля лучше вернуть ошибку или создать клиента на лету
            return NotFound(new { message = "Профиль клиента не найден. Обратитесь к администратору." });
        }

        // Формируем ответ вручную, чтобы избежать циклов и проблем с сериализацией
        var result = new
        {
            Id = user.Id,
            UserId = user.Id, // Для совместимости с моделью WPF
            Phone = user.Phone,
            Role = user.Role,
            ClientId = client.Id,
            FullName = client.FullName,
            Code = client.Code
        };

        System.Diagnostics.Debug.WriteLine($"[Server] Успешно возвращаем профиль для {user.Phone}");
        return Ok(result);
    }

    /// <summary>
    /// Обновление ФИО в таблице Clients
    /// URL: PUT /api/users/me/profile
    /// </summary>
    [HttpPut("me/profile")]
    public async Task<IActionResult> UpdateMyProfile([FromBody] ProfileUpdateDto dto)
    {
        var currentUserId = int.Parse(User.FindFirst("nameid")?.Value ?? "0");

        if (currentUserId == 0)
            return Unauthorized(new { message = "Некорректный токен" });

        // Валидация DTO
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Ищем клиента по UserId
        var client = await _db.Clients.FirstOrDefaultAsync(c => c.UserId == currentUserId);

        if (client == null)
            return NotFound(new { message = "Запись клиента не найдена. Возможно, профиль поврежден." });

        // Обновляем ФИО
        client.FullName = dto.FullName.Trim();

        await _db.SaveChangesAsync();

        return Ok(new { message = "ФИО успешно обновлено", fullName = client.FullName });
    }

    // --- АДМИНСКИЕ МЕТОДЫ ---

    /// <summary>
    /// [ADMIN] Получить список всех пользователей с их ролями
    /// URL: GET /api/users/all
    /// </summary>
    [HttpGet("all")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _db.Users
            .Include(u => u.Client) // Подгружаем клиента, если есть
            .Select(u => new
            {
                u.Id,
                u.Phone,
                u.Role,
                u.CreatedAt,
                FullName = u.Client != null ? u.Client.FullName : null,
                ClientId = u.Client != null ? u.Client.Id : (int?)null
            })
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        return Ok(users);
    }

    /// <summary>
    /// [ADMIN] Изменить роль пользователя
    /// URL: PUT /api/users/{id}/role
    /// Body: "driver" или "client" или "admin"
    /// </summary>
    [HttpPut("{id}/role")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> ChangeUserRole(int id, [FromBody] string newRole)
    {
        if (string.IsNullOrWhiteSpace(newRole))
            return BadRequest(new { message = "Роль не указана" });

        var allowedRoles = new[] { "client", "driver", "admin" };
        var roleLower = newRole.ToLower().Trim();

        if (!allowedRoles.Contains(roleLower))
            return BadRequest(new { message = $"Недопустимая роль. Разрешено: {string.Join(", ", allowedRoles)}" });

        var user = await _db.Users.FindAsync(id);
        if (user == null)
            return NotFound(new { message = "Пользователь не найден" });

        // Если меняем роль на driver, убедимся, что у него есть запись в drivers (опционально)
        // Если меняем на client, убедимся, что есть запись в clients (обычно есть при регистрации)

        user.Role = roleLower;
        await _db.SaveChangesAsync();

        return Ok(new { message = $"Роль пользователя изменена на '{roleLower}'" });
    }

    /// <summary>
    /// [ADMIN] Удалить пользователя (и связанные данные)
    /// URL: DELETE /api/users/{id}
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
            return NotFound(new { message = "Пользователь не найден" });

        // Проверка: нельзя удалить самого себя
        var currentUserId = int.Parse(User.FindFirst("nameid")?.Value ?? "0");
        if (currentUserId == id)
            return BadRequest(new { message = "Нельзя удалить собственную учетную запись" });

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Пользователь удален" });
    }
}