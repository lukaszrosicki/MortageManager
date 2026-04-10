using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MortgagePro.Infrastructure.Data;
using System.Security.Claims;
using System.Text.Json;

namespace MortgagePro.WebUI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ScenarioController : ControllerBase
{
    private readonly MortgageDbContext _db;

    public ScenarioController(MortgageDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult Get()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var scenarios = _db.Scenarios.Where(s => s.UserId == userId).ToList();
        
        var result = scenarios.Select(s => {
            ScenarioPayload payload = null;
            try {
                payload = JsonSerializer.Deserialize<ScenarioPayload>(s.SerializedSchedule, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            } catch {
                payload = new ScenarioPayload { Form = new object(), Schedule = JsonSerializer.Deserialize<JsonElement>("[]") };
            }
            return new {
                id = s.Id,
                name = s.Name,
                baselineInterest = s.BaselineInterest,
                data = payload
            };
        });
        return Ok(result);
    }

    [HttpPost]
    public IActionResult Post([FromBody] ScenarioRequest req)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        _db.Scenarios.Add(new ScenarioSnapshotEntity
        {
            Name = req.Name,
            BaselineInterest = req.BaselineInterest,
            SerializedSchedule = JsonSerializer.Serialize(req.Data),
            UserId = userId
        });
        _db.SaveChanges();
        return Ok();
    }

    [HttpPut("{id}")]
    public IActionResult Put(int id, [FromBody] ScenarioRequest req)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var scenario = _db.Scenarios.FirstOrDefault(s => s.Id == id && s.UserId == userId);
        if (scenario == null) return NotFound();

        scenario.Name = req.Name; 
        scenario.BaselineInterest = req.BaselineInterest;
        scenario.SerializedSchedule = JsonSerializer.Serialize(req.Data);
        _db.SaveChanges();
        return Ok();
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var scenario = _db.Scenarios.FirstOrDefault(s => s.Id == id && s.UserId == userId);
        if (scenario == null) return NotFound();

        _db.Scenarios.Remove(scenario);
        _db.SaveChanges();
        return Ok();
    }
}

public class ScenarioRequest
{
    public string Name { get; set; }
    public decimal BaselineInterest { get; set; }
    public ScenarioPayload Data { get; set; }
}

public class ScenarioPayload
{
    public object Form { get; set; }
    public JsonElement Schedule { get; set; }
}
