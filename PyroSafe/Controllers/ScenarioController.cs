using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// Шлях API: /api/scenarios
[Route("api/[controller]")]
[ApiController]
public class ScenarioController : ControllerBase
{
    private readonly AppDbContext _context;

    public ScenarioController(AppDbContext context)
    {
        _context = context;
    }

    // ---------------- GET /api/scenarios ----------------
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ScenarioReadDto>>> GetScenarios()
    {
        var scenarios = await _context.Scenarios
            .Select(s => new ScenarioReadDto
            {
                ID = s.ID,
                ScenarioType = s.ScenarioType,
                Description = s.Description,
                Priority = s.Priority,
                IsActive = s.IsActive
            })
            .ToListAsync();

        return Ok(scenarios);
    }

    // --------------- GET /api/scenarios/{id} ---------------
    [HttpGet("{id}")]
    public async Task<ActionResult<ScenarioReadDto>> GetScenario(int id)
    {
        var scenario = await _context.Scenarios
            .Where(s => s.ID == id)
            .Select(s => new ScenarioReadDto
            {
                ID = s.ID,
                ScenarioType = s.ScenarioType,
                Description = s.Description,
                Priority = s.Priority,
                IsActive = s.IsActive
            })
            .FirstOrDefaultAsync();

        if (scenario == null)
            return NotFound(new { message = "Scenario not found" });

        return Ok(scenario);
    }

    // --------------- POST /api/scenarios ---------------
    [HttpPost]
    public async Task<ActionResult<ScenarioReadDto>> CreateScenario([FromBody] ScenarioCreateDto scenarioDto)
    {
        var scenario = new Scenario
        {
            ScenarioType = scenarioDto.ScenarioType,
            Description = scenarioDto.Description,
            Priority = scenarioDto.Priority,
            IsActive = scenarioDto.IsActive
        };

        _context.Scenarios.Add(scenario);
        await _context.SaveChangesAsync();

        var resultDto = new ScenarioReadDto
        {
            ID = scenario.ID,
            ScenarioType = scenario.ScenarioType,
            Description = scenario.Description,
            Priority = scenario.Priority,
            IsActive = scenario.IsActive
        };

        return CreatedAtAction(nameof(GetScenario), new { id = scenario.ID }, resultDto);
    }

    // --------------- PUT /api/scenarios/{id} ---------------
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateScenario(int id, [FromBody] ScenarioCreateDto scenarioDto)
    {
        var scenario = await _context.Scenarios.FindAsync(id);
        if (scenario == null)
            return NotFound(new { message = "Scenario not found" });

        scenario.ScenarioType = scenarioDto.ScenarioType;
        scenario.Description = scenarioDto.Description;
        scenario.Priority = scenarioDto.Priority;
        scenario.IsActive = scenarioDto.IsActive;

        _context.Scenarios.Update(scenario);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // --------------- DELETE /api/scenarios/{id} ---------------
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteScenario(int id)
    {
        var scenario = await _context.Scenarios
            .Include(s => s.Events)
            .FirstOrDefaultAsync(s => s.ID == id);

        if (scenario == null)
            return NotFound(new { message = "Scenario not found" });

        if (scenario.Events.Any())
            return BadRequest(new { message = "Cannot delete scenario with associated events" });

        _context.Scenarios.Remove(scenario);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

// DTO для создания и обновления
public class ScenarioCreateDto
{
    public string ScenarioType { get; set; }
    public string Description { get; set; }
    public string Priority { get; set; }
    public bool IsActive { get; set; } = true;
}

// DTO для чтения (с ID)
public class ScenarioReadDto
{
    public int ID { get; set; }
    public string ScenarioType { get; set; }
    public string Description { get; set; }
    public string Priority { get; set; }
    public bool IsActive { get; set; }
}
