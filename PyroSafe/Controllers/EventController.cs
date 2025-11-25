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
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        var result = events.Select(e => new
        {
            id = e.ID,
            sensorID = e.SensorID,
            sensorName = e.Sensor != null ? $"{e.Sensor.SensorName} ({e.Sensor.SensorType})" : "Невідомий сенсор",
            scenarioID = e.ScenarioID,
            scenarioName = e.Scenario?.ScenarioType ?? "—",
            description = e.Description ?? "Без опису",
            severity = e.Severity ?? "Info",
            status = e.Status ?? "New",
            createdAt = e.CreatedAt,
            createdBy = CurrentUserId,
            createdByName = CurrentUserName
        });

        return Ok(result);
    }

    // POST: api/events
    [HttpPost]
    public async Task<IActionResult> CreateEvent([FromBody] EventCreateDto dto)
    {
        if (dto == null || dto.sensorID <= 0)
            return BadRequest("Оберіть сенсор");

        if (string.IsNullOrWhiteSpace(dto.description))
            return BadRequest("Введіть опис");

        var sensorExists = await _context.Sensors.AnyAsync(s => s.ID == dto.sensorID);
        if (!sensorExists)
            return BadRequest("Сенсор не знайдено");

        var ev = new Event
        {
            SensorID = dto.sensorID,
            ScenarioID = dto.scenarioID,
            Description = dto.description.Trim(),
            Severity = dto.severity?.Trim() ?? "Info",
            Status = dto.status?.Trim() ?? "New",
            EventTime = DateTime.Now,
            CreatedAt = DateTime.Now   // ← використовуємо твоє поле!
        };

        _context.Events.Add(ev);
        await _context.SaveChangesAsync();

        var sensor = await _context.Sensors.FirstOrDefaultAsync(s => s.ID == ev.SensorID);
        var scenario = dto.scenarioID != null
            ? await _context.Scenarios.FirstOrDefaultAsync(s => s.ID == dto.scenarioID)
            : null;

        var result = new
        {
            id = ev.ID,
            sensorID = ev.SensorID,
            sensorName = sensor != null ? $"{sensor.SensorName} ({sensor.SensorType})" : "Невідомий",
            scenarioID = ev.ScenarioID,
            scenarioName = scenario?.ScenarioType ?? "—",
            description = ev.Description,
            severity = ev.Severity,
            status = ev.Status,
            createdAt = ev.CreatedAt,
            createdBy = CurrentUserId,
            createdByName = CurrentUserName
        };

        return CreatedAtAction(nameof(GetEvent), new { id = ev.ID }, result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var ev = await _context.Events.FindAsync(id);
        if (ev == null) return NotFound();

        _context.Events.Remove(ev);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetEvent(int id)
    {
        var ev = await _context.Events
            .Include(e => e.Sensor)
            .Include(e => e.Scenario)
            .FirstOrDefaultAsync(e => e.ID == id);

        if (ev == null) return NotFound();

        return Ok(new
        {
            id = ev.ID,
            sensorID = ev.SensorID,
            sensorName = ev.Sensor != null ? $"{ev.Sensor.SensorName} ({ev.Sensor.SensorType})" : "Невідомий",
            scenarioID = ev.ScenarioID,
            scenarioName = ev.Scenario?.ScenarioType ?? "—",
            description = ev.Description,
            severity = ev.Severity,
            status = ev.Status,
            createdAt = ev.CreatedAt,
            createdBy = CurrentUserId,
            createdByName = CurrentUserName
        });
    }
}

// DTO — camelCase
public class EventCreateDto
{
    public int sensorID { get; set; }
    public int? scenarioID { get; set; }
    public string description { get; set; } = "";
    public string? severity { get; set; }
    public string? status { get; set; }
}