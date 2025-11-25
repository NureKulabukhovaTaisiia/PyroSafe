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

    private int CurrentUserId =>
        int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var id) ? id :
        int.TryParse(HttpContext.Session.GetString("UserId"), out var sid) ? sid : 1;

    private string CurrentUserName => User.Identity?.Name ?? "Охоронець";

    // GET: api/events
    [HttpGet]
    public async Task<IActionResult> GetEvents()
    {
        var events = await _context.Events
            .Include(e => e.Sensor)
            .Include(e => e.Scenario)
            .Include(e => e.ResolvedUser) // підтягуємо ім’я того, хто вирішив
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
            createdBy = CurrentUserId,
            createdByName = CurrentUserName,
            resolvedAt = e.ResolvedAt,
            resolvedBy = e.ResolvedBy,
            resolvedByName = e.ResolvedUser != null ? e.ResolvedUser.Username : null
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

        var sensorExists = await _context.Sensors.AnyAsync(s => s.ID == dto.SensorID);
        if (!sensorExists)
            return BadRequest("Сенсор не знайдено");

        var ev = new Event
        {
            SensorID = dto.SensorID,
            ScenarioID = dto.ScenarioID,
            Description = dto.Description.Trim(),
            Severity = dto.Severity?.Trim() ?? "Info",
            Status = dto.Status?.Trim() ?? "New",
            EventTime = DateTime.Now,
            CreatedAt = DateTime.Now
            // ResolvedAt і ResolvedBy залишаються null — івент ще не вирішений
        };

        _context.Events.Add(ev);
        await _context.SaveChangesAsync();

        var sensor = await _context.Sensors.FirstOrDefaultAsync(s => s.ID == ev.SensorID);
        var scenario = dto.ScenarioID != null
            ? await _context.Scenarios.FirstOrDefaultAsync(s => s.ID == dto.ScenarioID)
            : null;

        var result = new
        {
            id = ev.ID,
            SensorID = ev.SensorID,
            sensorName = sensor != null ? $"{sensor.SensorName} ({sensor.SensorType})" : "Невідомий",
            ScenarioID = ev.ScenarioID,
            scenarioName = scenario?.ScenarioType ?? "—",
            Description = ev.Description,
            Severity = ev.Severity,
            Status = ev.Status,
            createdAt = ev.CreatedAt,
            createdBy = CurrentUserId,
            createdByName = CurrentUserName,
            resolvedAt = ev.ResolvedAt,
            resolvedBy = ev.ResolvedBy,
            resolvedByName = (string?)null
        };

        return CreatedAtAction(nameof(GetEvent), new { id = ev.ID }, result);
    }

    // НОВИЙ МЕТОД: Позначити івент як вирішений
    [HttpPatch("{id}/resolve")]
    public async Task<IActionResult> ResolveEvent(int id)
    {
        var ev = await _context.Events.FindAsync(id);
        if (ev == null)
            return NotFound();

        if (ev.Status == "Resolved")
            return BadRequest("Івент вже вирішений");

        ev.Status = "Resolved";
        ev.ResolvedAt = DateTime.Now;
        ev.ResolvedBy = CurrentUserId;

        await _context.SaveChangesAsync();

        // Повертаємо оновлений івент
        var resolvedUser = await _context.Users.FindAsync(CurrentUserId);

        return Ok(new
        {
            message = "Івент успішно позначено як вирішений",
            eventId = ev.ID,
            resolvedAt = ev.ResolvedAt,
            resolvedBy = ev.ResolvedBy,
            resolvedByName = resolvedUser?.Username ?? CurrentUserName
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
        return NoContent();
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
            sensorName = ev.Sensor != null ? $"{ev.Sensor.SensorName} ({ev.Sensor.SensorType})" : "Невідомий",
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

public class EventCreateDto
{
    public int SensorID { get; set; }
    public int? ScenarioID { get; set; }
    public string Description { get; set; } = "";
    public string? Severity { get; set; }
    public string? Status { get; set; }
}