using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// Шлях API: /api/zones
[Route("api/[controller]")]
[ApiController]
public class ZoneController : ControllerBase
{
    private readonly AppDbContext _context;

    public ZoneController(AppDbContext context)
    {
        _context = context;
    }

    // GET /api/zones
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ZoneReadDto>>> GetZones()
    {
        var zones = await _context.Zones
            .Select(z => new ZoneReadDto
            {
                ID = z.ID,
                ZoneName = z.ZoneName,
                Floor = z.Floor,
                Area = z.Area
            })
            .ToListAsync();

        return Ok(zones);
    }

    // GET /api/zones/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<ZoneReadDto>> GetZone(int id)
    {
        var zone = await _context.Zones
            .Where(z => z.ID == id)
            .Select(z => new ZoneReadDto
            {
                ID = z.ID,
                ZoneName = z.ZoneName,
                Floor = z.Floor,
                Area = z.Area
            })
            .FirstOrDefaultAsync();

        if (zone == null) return NotFound();
        return Ok(zone);
    }


    // POST /api/zones
    [HttpPost]
    public async Task<ActionResult<ZoneReadDto>> CreateZone([FromBody] ZoneCreateDto zoneDto)
    {
        var zone = new Zone
        {
            ZoneName = zoneDto.ZoneName,
            Floor = zoneDto.Floor,
            Area = zoneDto.Area
        };

        _context.Zones.Add(zone);
        await _context.SaveChangesAsync();

        var resultDto = new ZoneReadDto
        {
            ID = zone.ID,
            ZoneName = zone.ZoneName,
            Floor = zone.Floor,
            Area = zone.Area
        };

        return CreatedAtAction(nameof(GetZone), new { id = zone.ID }, resultDto);
    }



    // --------------- PUT /api/zones/{id} ---------------
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateZone(int id, [FromBody] ZoneReadDto zoneDto)
    {
        var zone = await _context.Zones.FindAsync(id);
        if (zone == null)
            return NotFound(new { message = "Zone not found" });

        zone.ZoneName = zoneDto.ZoneName;
        zone.Floor = zoneDto.Floor;
        zone.Area = zoneDto.Area;

        _context.Zones.Update(zone);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // --------------- DELETE /api/zones/{id} ---------------
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteZone(int id)
    {
        var zone = await _context.Zones
            .Include(z => z.Sensors)
            .ThenInclude(s => s.Events)
            .FirstOrDefaultAsync(z => z.ID == id);

        if (zone == null)
            return NotFound(new { message = "Zone not found" });

        bool hasActiveSensors = zone.Sensors.Any(s => s.Status == "Active");
        bool hasEvents = zone.Sensors.Any(s => s.Events.Any());

        if (hasActiveSensors || hasEvents)
            return BadRequest(new { message = "Cannot delete zone with active sensors or events" });

        _context.Zones.Remove(zone);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

// DTO без сенсоров
public class ZoneCreateDto
{
    public string ZoneName { get; set; }
    public int Floor { get; set; }
    public double Area { get; set; }
}

// DTO для чтения (с ID)
public class ZoneReadDto
{
    public int ID { get; set; }
    public string ZoneName { get; set; }
    public int Floor { get; set; }
    public double Area { get; set; }
}