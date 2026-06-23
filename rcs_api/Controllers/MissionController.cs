using Microsoft.AspNetCore.Mvc;
using rcs_api.Models;
using rcs_api.Services;

namespace rcs_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MissionController : ControllerBase
{
    private readonly MissionQueueService _queue;
    private readonly RosbridgeClientService _rosbridge;

    public MissionController(MissionQueueService queue, RosbridgeClientService rosbridge)
    {
        _queue = queue;
        _rosbridge = rosbridge;
    }

    // 임무 목록 조회
    [HttpGet]
    public IActionResult GetAll() => Ok(_queue.GetAll());

    // 임무 등록
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMissionRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.TargetLocation))
            return BadRequest("TargetLocation is required. Format: \"x,y\"");

        var mission = _queue.Enqueue(req.TargetLocation);

        // Pending 상태인 임무가 이것 하나면 바로 실행
        if (_queue.GetAll().Count(m => m.Status == MissionStatus.Running) == 0)
        {
            _queue.UpdateStatus(mission.Id, MissionStatus.Running);
            await _rosbridge.PublishGoalAsync(mission.TargetLocation);
        }

        return CreatedAtAction(nameof(GetById), new { id = mission.Id }, mission);
    }

    // 단일 임무 조회
    [HttpGet("{id}")]
    public IActionResult GetById(Guid id)
    {
        var mission = _queue.GetAll().FirstOrDefault(m => m.Id == id);
        return mission is null ? NotFound() : Ok(mission);
    }

    // 임무 상태 업데이트
    [HttpPatch("{id}/status")]
    public IActionResult UpdateStatus(Guid id, [FromBody] UpdateStatusRequest req)
    {
        if (!Enum.TryParse<MissionStatus>(req.Status, out var status))
            return BadRequest("Invalid status");

        return _queue.UpdateStatus(id, status) ? NoContent() : NotFound();
    }

    // AGV 현재 상태 조회
    [HttpGet("/api/status")]
    public IActionResult GetAgvStatus() => Ok(_rosbridge.GetStatus());
}

public record CreateMissionRequest(string TargetLocation);
public record UpdateStatusRequest(string Status);
