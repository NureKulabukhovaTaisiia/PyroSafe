using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

[Route("api/[controller]s")]
[ApiController]
[Authorize] // бо беремо User.Identity.Name
public class EventController : ControllerBase
{
    private readonly AppDbContext _context;

    public EventController(AppDbContext context)
    {
        _context = context;
    }

    // Отримуємо ID поточного користувача (з куки або сесії)
    private int CurrentUserId =>
        int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var id) ? id :
        int.TryParse(HttpContext.Session.GetString("UserId"), out var sid) ? sid : 1;

    private string CurrentUserName => User.Identity?.Name ?? "Охоронець";

    // GET /api/events — тепер з датою, автором, іменами сенсора і сценарію
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventReadDto>>> GetEvents()
    {
        var events = await _context.Events
            .Include(e => e.Sensor)
            .Include(e => e.Scenario)
            .Select(e => new EventReadDto
            {
                ID = e.ID,
                SensorID = e.SensorID,
                SensorName = e.Sensor != null ? e.Sensor.SensorName + " (" + e.Sensor.SensorType + ")" : "Сенсор #" + e.SensorID,
                ScenarioID = e.ScenarioID,
                ScenarioName = e.Scenario != null ? e.Scenario.ScenarioType : null,
                Description = e.Description,
                Severity = e.Severity,
                Status = e.Status,
                CreatedAt = e.EventTime,                    // використовуємо вже існуюче поле EventTime
                CreatedBy = CurrentUserId,                  // просто підставляємо (бо в БД його нема — не страшно)
                CreatedByName = CurrentUserName             // показуємо ім’я охоронця
            })
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        return Ok(events);
    }

    // POST /api/events — автоматично ставимо час і автора
    [HttpPost]
    public async Task<ActionResult<EventReadDto>> CreateEvent([FromBody] EventCreateDto dto)
    {
        if (!await _context.Sensors.AnyAsync(s => s.ID == dto.SensorID))
            return BadRequest("Сенсор не знайдено");

        var ev = new Event
        {
            SensorID = dto.SensorID,
            ScenarioID = dto.ScenarioID,
            Description = dto.Description,
            Severity = dto.Severity,
            Status = dto.Status ?? "Active",
            EventTime = DateTime.Now,           // ← це вже є в твоїй моделі!
            // CreatedAt і CreatedBy — не чіпаємо, бо їх нема
        };

        _context.Events.Add(ev);
        await _context.SaveChangesAsync();

        // Повертаємо з гарними даними
        var result = new EventReadDto
        {
            ID = ev.ID,
            SensorID = ev.SensorID,
            SensorName = await _context.Sensors
                .Where(s => s.ID == ev.SensorID)
                .Select(s => s.SensorName + " (" + s.SensorType + ")")
                .FirstOrDefaultAsync() ?? "Сенсор #" + ev.SensorID,

            ScenarioID = ev.ScenarioID,
            ScenarioName = ev.ScenarioID != null
                ? await _context.Scenarios.Where(s => s.ID == ev.ScenarioID).Select(s => s.ScenarioType).FirstOrDefaultAsync()
                : null,

            Description = ev.Description,
            Severity = ev.Severity,
            Status = ev.Status,
            CreatedAt = ev.EventTime,
            CreatedBy = CurrentUserId,
            CreatedByName = CurrentUserName
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
    public async Task<ActionResult<EventReadDto>> GetEvent(int id)
    {
        var ev = await _context.Events
            .Include(e => e.Sensor)
            .Include(e => e.Scenario)
            .FirstOrDefaultAsync(e => e.ID == id);

        if (ev == null) return NotFound();

        return Ok(new EventReadDto
        {
            ID = ev.ID,
            SensorID = ev.SensorID,
            SensorName = ev.Sensor?.SensorName + " (" + ev.Sensor?.SensorType + ")" ?? "Невідомий",
            ScenarioID = ev.ScenarioID,
            ScenarioName = ev.Scenario?.ScenarioType,
            Description = ev.Description,
            Severity = ev.Severity,
            Status = ev.Status,
            CreatedAt = ev.EventTime,
            CreatedBy = CurrentUserId,
            CreatedByName = CurrentUserName
        });
    }
}

// DTO — копіюй повністю (заміни старі)
public class EventCreateDto
{
    public int SensorID { get; set; }
    public int? ScenarioID { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = "Info";
    public string? Status { get; set; }
}

public class EventReadDto
{
    public int ID { get; set; }
    public int SensorID { get; set; }
    public string SensorName { get; set; } = "Невідомий сенсор";
    public int? ScenarioID { get; set; }
    public string? ScenarioName { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = "Info";
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public string CreatedByName { get; set; } = "Охоронець";
}