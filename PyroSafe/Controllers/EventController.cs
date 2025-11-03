using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class EventController : ControllerBase
{
    private readonly AppDbContext _context;

    public EventController(AppDbContext context)
    {
        _context = context;
    }

    // GET /api/events
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventReadDto>>> GetEvents()
    {
        var events = await _context.Events
            .Select(e => new EventReadDto
            {
                ID = e.ID,
                SensorID = e.SensorID,
                ScenarioID = e.ScenarioID,
                Description = e.Description,
                Severity = e.Severity,
                Status = e.Status,
                ResolvedBy = e.ResolvedBy,
                ResolvedAt = e.ResolvedAt
            })
            .ToListAsync();

        return Ok(events);
    }

    // GET /api/events/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<EventReadDto>> GetEvent(int id)
    {
        var ev = await _context.Events
            .Where(e => e.ID == id)
            .Select(e => new EventReadDto
            {
                ID = e.ID,
                SensorID = e.SensorID,
                ScenarioID = e.ScenarioID,
                Description = e.Description,
                Severity = e.Severity,
                Status = e.Status,
                ResolvedBy = e.ResolvedBy,
                ResolvedAt = e.ResolvedAt
            })
            .FirstOrDefaultAsync();

        if (ev == null) return NotFound();
        return Ok(ev);
    }

    // POST /api/events
    [HttpPost]
    public async Task<ActionResult<EventReadDto>> CreateEvent([FromBody] EventCreateDto dto)
    {
        var sensor = await _context.Sensors.FindAsync(dto.SensorID);
        if (sensor == null) return BadRequest(new { message = "Sensor not found" });

        var ev = new Event
        {
            SensorID = dto.SensorID,
            ScenarioID = dto.ScenarioID,
            Description = dto.Description,
            Severity = dto.Severity,
            Status = dto.Status,
            ResolvedBy = dto.ResolvedBy,
            ResolvedAt = dto.ResolvedAt
        };

        _context.Events.Add(ev);
        await _context.SaveChangesAsync();

        var resultDto = new EventReadDto
        {
            ID = ev.ID,
            SensorID = ev.SensorID,
            ScenarioID = ev.ScenarioID,
            Description = ev.Description,
            Severity = ev.Severity,
            Status = ev.Status,
            ResolvedBy = ev.ResolvedBy,
            ResolvedAt = ev.ResolvedAt
        };

        return CreatedAtAction(nameof(GetEvent), new { id = ev.ID }, resultDto);
    }

    // PUT /api/events/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEvent(int id, [FromBody] EventCreateDto dto)
    {
        var ev = await _context.Events.FindAsync(id);
        if (ev == null) return NotFound(new { message = "Event not found" });

        ev.SensorID = dto.SensorID;
        ev.ScenarioID = dto.ScenarioID;
        ev.Description = dto.Description;
        ev.Severity = dto.Severity;
        ev.Status = dto.Status;
        ev.ResolvedBy = dto.ResolvedBy;
        ev.ResolvedAt = dto.ResolvedAt;

        _context.Events.Update(ev);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE /api/events/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var ev = await _context.Events.FindAsync(id);
        if (ev == null) return NotFound(new { message = "Event not found" });

        _context.Events.Remove(ev);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

// DTO для создания события
public class EventCreateDto
{
    public int SensorID { get; set; }
    public int? ScenarioID { get; set; }
    public string Description { get; set; }
    public string Severity { get; set; }
    public string Status { get; set; }
    public int? ResolvedBy { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

// DTO для чтения события
public class EventReadDto
{
    public int ID { get; set; }
    public int SensorID { get; set; }
    public int? ScenarioID { get; set; }
    public string Description { get; set; }
    public string Severity { get; set; }
    public string Status { get; set; }
    public int? ResolvedBy { get; set; }
    public DateTime? ResolvedAt { get; set; }
}
