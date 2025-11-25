using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// Шлях API: /api/sensors
[Route("api/[controller]")]
[ApiController]
public class SensorsController : ControllerBase  // ← Правильна назва!
{
    private readonly AppDbContext _context;

    public SensorsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SensorReadDto>>> GetSensors()
    {
        var sensors = await _context.Sensors
            .Select(s => new SensorReadDto
            {
                ID = s.ID,
                SensorName = s.SensorName,
                SensorValue = s.SensorValue,
                SensorType = s.SensorType,
                Status = s.Status ?? "Active",
                ZoneID = s.ZoneID
            })
            .ToListAsync();

        return Ok(sensors);
    }

    [HttpPost]
    public async Task<ActionResult<SensorReadDto>> CreateSensor([FromBody] SensorCreateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var zoneExists = await _context.Zones.AnyAsync(z => z.ID == dto.ZoneID);
        if (!zoneExists)
            return BadRequest(new { message = "Зона з таким ID не існує" });

        var sensor = new Sensor
        {
            SensorName = dto.SensorName,
            SensorValue = dto.SensorValue ?? "0",
            SensorType = dto.SensorType,
            Status = dto.Status ?? "Active",
            ZoneID = dto.ZoneID
        };

        _context.Sensors.Add(sensor);
        await _context.SaveChangesAsync();

        var readDto = new SensorReadDto
        {
            ID = sensor.ID,
            SensorName = sensor.SensorName,
            SensorValue = sensor.SensorValue,
            SensorType = sensor.SensorType,
            Status = sensor.Status,
            ZoneID = sensor.ZoneID
        };

        return CreatedAtAction(nameof(GetSensor), new { id = sensor.ID }, readDto);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SensorReadDto>> GetSensor(int id)
    {
        var sensor = await _context.Sensors.FindAsync(id);
        if (sensor == null) return NotFound();

        return Ok(new SensorReadDto
        {
            ID = sensor.ID,
            SensorName = sensor.SensorName,
            SensorValue = sensor.SensorValue,
            SensorType = sensor.SensorType,
            Status = sensor.Status,
            ZoneID = sensor.ZoneID
        });
    }


    // --------------- PUT /api/sensors/{id} ---------------
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSensor(int id, [FromBody] SensorCreateDto sensorDto)
    {
        var sensor = await _context.Sensors.FindAsync(id);
        if (sensor == null)
            return NotFound(new { message = "Sensor not found" });

        sensor.SensorName = sensorDto.SensorName;
        sensor.SensorValue = sensorDto.SensorValue;
        sensor.SensorType = sensorDto.SensorType;
        sensor.Status = sensorDto.Status;
        sensor.ZoneID = sensorDto.ZoneID;

        _context.Sensors.Update(sensor);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // --------------- DELETE /api/sensors/{id} ---------------
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSensor(int id)
    {
        var sensor = await _context.Sensors
            .Include(s => s.Events)
            .FirstOrDefaultAsync(s => s.ID == id);

        if (sensor == null)
            return NotFound(new { message = "Sensor not found" });

        if (sensor.Events.Any())
            return BadRequest(new { message = "Cannot delete sensor with events" });

        _context.Sensors.Remove(sensor);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

public class SensorCreateDto
{
    public string SensorName { get; set; }
    public string SensorValue { get; set; }
    public string SensorType { get; set; }
    public string Status { get; set; }
    public int ZoneID { get; set; }
}


public class SensorReadDto
{
    public int ID { get; set; }
    public string SensorName { get; set; }
    public string SensorValue { get; set; }
    public string SensorType { get; set; }
    public string Status { get; set; }
    public int ZoneID { get; set; }
}