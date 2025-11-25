using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/events")]
[ApiController]
[Authorize]
public class EventController : ControllerBase
{
    private readonly AppDbContext _context;

    public EventController(AppDbContext context)
    {
        _context = context;
    }

    // Надійне отримання ID користувача (з куки → сесія → 1)
    private int CurrentUserId
    {
        get
        {
            // Спочатку — з Claims (якщо використовуєш JWT або Identity)
            var claimId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(claimId, out var idFromClaim)) return idFromClaim;

            // Потім — з сесії (твій старий спосіб)
            var sessionId = HttpContext.Session.GetString("UserId");
            if (int.TryParse(sessionId, out var idFromSession)) return idFromSession;

            // Якщо нічого немає — тимчасовий користувач
            return 1;
        }
    }

    private string CurrentUserName => User.Identity?.Name ?? "Охоронець";

    // GET: api/events
    [HttpGet]
    public async Task<IActionResult> GetEvents()
    {
        var events = await _context.Events
            .Include(e => e.Sensor)
            .Include(e => e.Scenario)
            .Include(e => e.ResolvedUser)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        var result = events.Select(e => new
        {
            id = e.ID,
            SensorID = e.SensorID,
            sensorName = e.Sensor != null ? $"{e.Sensor.SensorName} ({e.Sensor.SensorType})" : "Невідомий сенсор",
            ScenarioID = e.ScenarioID,
            scenarioName = e.Scenario?.ScenarioType ?? "—",
            Description = e.Description ?? "Без опису",
            Severity = e.Severity ?? "Info",
            Status = e.Status ?? "New",
            createdAt = e.CreatedAt,
            createdBy = CurrentUserId,           // додаємо в JSON, хоч і не зберігаємо в БД
            createdByName = CurrentUserName,
            resolvedAt = e.ResolvedAt,
            resolvedBy = e.ResolvedBy,
            resolvedByName = e.ResolvedUser?.Username
        });

        return Ok(result);
    }

    // POST: api/events
    [HttpPost]
    public async Task<IActionResult> CreateEvent([FromBody] EventCreateDto dto)
    {
        if (dto == null || dto.SensorID <= 0)
            return BadRequest("Оберіть сенсор");

        if (string.IsNullOrWhiteSpace(dto.Description))
            return BadRequest("Введіть опис");

        if (!await _context.Sensors.AnyAsync(s => s.ID == dto.SensorID))
            return BadRequest("Сенсор не знайдено");

        var ev = new Event
        {
            SensorID = dto.SensorID,
            ScenarioID = dto.ScenarioID,
            Description = dto.Description.Trim(),
            Severity = dto.Severity?.Trim() ?? "Info",
            Status = dto.Status?.Trim() ?? "New",
            EventTime = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
            // CreatedBy НЕ зберігаємо в БД — немає поля!
        };

        _context.Events.Add(ev);
        await _context.SaveChangesAsync();

        // Повертаємо простий успіх — НЕ викликаємо GetEvent (щоб не було 500)
        return Ok(new
        {
            success = true,
            id = ev.ID,
            message = "Івент успішно створено"
        });
    }

    // PATCH: api/events/{id}/resolve
    [HttpPatch("{id}/resolve")]
    public async Task<IActionResult> ResolveEvent(int id)
    {
        var ev = await _context.Events.FindAsync(id);
        if (ev == null) return NotFound();
        if (ev.Status == "Resolved") return BadRequest("Івент вже вирішений");

        ev.Status = "Resolved";
        ev.ResolvedAt = DateTime.UtcNow;
        ev.ResolvedBy = CurrentUserId;

        await _context.SaveChangesAsync();

        var user = await _context.Users.FindAsync(CurrentUserId);

        return Ok(new
        {
            success = true,
            message = "Івент позначено як вирішений",
            resolvedByName = user?.Username ?? CurrentUserName
        });
    }

    // DELETE: api/events/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var ev = await _context.Events.FindAsync(id);
        if (ev == null) return NotFound();

        _context.Events.Remove(ev);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Івент видалено" });
    }

    // GET: api/events/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetEvent(int id)
    {
        var ev = await _context.Events
            .Include(e => e.Sensor)
            .Include(e => e.Scenario)
            .Include(e => e.ResolvedUser)
            .FirstOrDefaultAsync(e => e.ID == id);

        if (ev == null) return NotFound();

        return Ok(new
        {
            id = ev.ID,
            SensorID = ev.SensorID,
            sensorName = ev.Sensor != null ? $"{e.Sensor.SensorName} ({e.Sensor.SensorType})" : "Невідомий",
            ScenarioID = ev.ScenarioID,
            scenarioName = ev.Scenario?.ScenarioType ?? "—",
            Description = ev.Description,
            Severity = ev.Severity,
            Status = ev.Status,
            createdAt = ev.CreatedAt,
            createdBy = CurrentUserId,
            createdByName = CurrentUserName,
            resolvedAt = ev.ResolvedAt,
            resolvedBy = ev.ResolvedBy,
            resolvedByName = ev.ResolvedUser?.Username
        });
    }
}

// DTO — PascalCase, як у БД
public class EventCreateDto
{
    public int SensorID { get; set; }
    public int? ScenarioID { get; set; }
    public string Description { get; set; } = "";
    public string? Severity { get; set; }
    public string? Status { get; set; }
}