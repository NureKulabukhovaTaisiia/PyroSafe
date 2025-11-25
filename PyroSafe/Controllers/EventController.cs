using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/[controller]s")]
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

    // GET: api/events — список усіх івентів
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventReadDto>>> GetEvents()
    {
        var eventsFromDb = await _context.Events
            .Include(e => e.Sensor)
            .Include(e => e.Scenario)
            .OrderByDescending(e => e.EventTime)
            .Select(e => new
            {
                e.ID,
                e.SensorID,
                SensorName = e.Sensor != null ? e.Sensor.SensorName : null,
                SensorType = e.Sensor != null ? e.Sensor.SensorType : null,
                e.ScenarioID,
                ScenarioName = e.Scenario != null ? e.Scenario.ScenarioType : null,
                e.Description,
                e.Severity,
                e.Status,
                e.EventTime
            })
            .ToListAsync();

        var result = eventsFromDb.Select(e => new EventReadDto
        {
            id = e.ID,
            sensorID = e.SensorID,
            sensorName = e.SensorName != null
                ? $"{e.SensorName} ({e.SensorType})"
                : "Сенсор #" + e.SensorID,
            scenarioID = e.ScenarioID,
            scenarioName = e.ScenarioName,
            description = e.Description ?? "",
            severity = e.Severity ?? "Info",
            status = e.Status ?? "Active",
            createdAt = e.EventTime,
            createdBy = CurrentUserId,
            createdByName = CurrentUserName
        });

        return Ok(result);
    }

    // GET: api/events/5 — один івент
    [HttpGet("{id}")]
    public async Task<ActionResult<EventReadDto>> GetEvent(int id)
    {
        var ev = await _context.Events
            .Include(e => e.Sensor)
            .Include(e => e.Scenario)
            .FirstOrDefaultAsync(e => e.ID == id);

        if (ev == null) return NotFound();

        var dto = new EventReadDto
        {
            id = ev.ID,
            sensorID = ev.SensorID,
            sensorName = ev.Sensor != null
                ? $"{ev.Sensor.SensorName} ({ev.Sensor.SensorType})"
                : "Сенсор #" + ev.SensorID,
            scenarioID = ev.ScenarioID,
            scenarioName = ev.Scenario?.ScenarioType,
            description = ev.Description ?? "",
            severity = ev.Severity ?? "Info",
            status = ev.Status ?? "Active",
            createdAt = ev.EventTime,
            createdBy = CurrentUserId,
            createdByName = CurrentUserName
        };

        return Ok(dto);
    }

    // POST: api/events — створення івенту
    [HttpPost]
    public async Task<ActionResult<EventReadDto>> CreateEvent([FromBody] EventCreateDto dto)
    {
        if (dto == null) return BadRequest("Дані не передані");
        if (dto.sensorID <= 0) return BadRequest("Оберіть сенсор");
        if (string.IsNullOrWhiteSpace(dto.description)) return BadRequest("Введіть опис");

        var sensorExists = await _context.Sensors.AnyAsync(s => s.ID == dto.sensorID);
        if (!sensorExists) return BadRequest("Сенсор не знайдено");

        var ev = new Event
        {
            SensorID = dto.sensorID,
            ScenarioID = dto.scenarioID,
            Description = dto.description.Trim(),
            Severity = dto.severity ?? "Info",
            Status = dto.status ?? "Active",
            EventTime = DateTime.Now
        };

        _context.Events.Add(ev);
        await _context.SaveChangesAsync();

        // Підтягуємо імена
        var sensor = await _context.Sensors.FirstOrDefaultAsync(s => s.ID == ev.SensorID);
        var scenario = dto.scenarioID != null
            ? await _context.Scenarios.FirstOrDefaultAsync(s => s.ID == dto.scenarioID)
            : null;

        var result = new EventReadDto
        {
            id = ev.ID,
            sensorID = ev.SensorID,
            sensorName = sensor != null ? $"{sensor.SensorName} ({sensor.SensorType})" : "Невідомий",
            scenarioID = ev.ScenarioID,
            scenarioName = scenario?.ScenarioType,
            description = ev.Description,
            severity = ev.Severity,
            status = ev.Status,
            createdAt = ev.EventTime,
            createdBy = CurrentUserId,
            createdByName = CurrentUserName
        };

        return CreatedAtAction(nameof(GetEvent), new { id = ev.ID }, result);
        // ↑ Тепер GetEvent точно існує!
    }

    // DELETE: api/events/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var ev = await _context.Events.FindAsync(id);
        if (ev == null) return NotFound();

        _context.Events.Remove(ev);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

// DTO — camelCase, щоб фронтенд розумів
public class EventCreateDto
{
    public int sensorID { get; set; }
    public int? scenarioID { get; set; }
    public string description { get; set; } = "";
    public string? severity { get; set; }
    public string? status { get; set; }
}

public class EventReadDto
{
    public int id { get; set; }
    public int sensorID { get; set; }
    public string sensorName { get; set; } = "Невідомий сенсор";
    public int? scenarioID { get; set; }
    public string? scenarioName { get; set; }
    public string description { get; set; } = "";
    public string severity { get; set; } = "Info";
    public string status { get; set; } = "Active";
    public DateTime createdAt { get; set; }
    public int createdBy { get; set; }
    public string createdByName { get; set; } = "Охоронець";
}