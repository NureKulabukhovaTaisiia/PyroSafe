using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// Шлях API: /api/sensors
[Route("api/[controller]")]
[ApiController]
public class SensorController : ControllerBase
{
    private readonly AppDbContext _context;

    public SensorController(AppDbContext context)
    {
        _context = context;
    }

    // GET /api/sensors
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
                Status = s.Status,
                ZoneID = s.ZoneID
            })
            .ToListAsync();

        return Ok(sensors);
    }

    // GET /api/sensors/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<SensorReadDto>> GetSensor(int id)
    {
        var sensor = await _context.Sensors
            .Where(s => s.ID == id)
            .Select(s => new SensorReadDto
            {
                ID = s.ID,
                SensorName = s.SensorName,
                SensorValue = s.SensorValue,
                SensorType = s.SensorType,
                Status = s.Status,
                ZoneID = s.ZoneID
            })
            .FirstOrDefaultAsync();

        if (sensor == null)
            return NotFound();

        return Ok(sensor);
    }



    // --------------- POST /api/sensors ---------------
    [HttpPost]
    public async Task<ActionResult<SensorReadDto>> CreateSensor([FromBody] SensorCreateDto sensorDto)
    {
        var zone = await _context.Zones.FindAsync(sensorDto.ZoneID);
        if (zone == null)
            return BadRequest(new { message = "Zone not found" });

        var sensor = new Sensor
        {
            SensorName = sensorDto.SensorName,
            SensorValue = sensorDto.SensorValue,
            SensorType = sensorDto.SensorType,
            ZoneID = sensorDto.ZoneID,
            Status = sensorDto.Status
        };

        _context.Sensors.Add(sensor);
        await _context.SaveChangesAsync();

        var resultDto = new SensorReadDto
        {
            ID = sensor.ID,
            SensorName = sensor.SensorName,
            SensorValue = sensor.SensorValue,
            SensorType = sensor.SensorType,
            Status = sensor.Status,
            ZoneID = sensor.ZoneID
        };

        return CreatedAtAction(nameof(GetSensor), new { id = sensor.ID }, resultDto);
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