using rcs_api.Models;

namespace rcs_api.Services;

public class MissionQueueService
{
    private readonly List<Mission> _missions = new();
    private readonly object _lock = new();

    public Mission Enqueue(string targetLocation)
    {
        var mission = new Mission { TargetLocation = targetLocation };
        lock (_lock) { _missions.Add(mission); }
        return mission;
    }

    public Mission? GetNext()
    {
        lock (_lock)
            return _missions.FirstOrDefault(m => m.Status == MissionStatus.Pending);
    }

    public IReadOnlyList<Mission> GetAll()
    {
        lock (_lock) return _missions.AsReadOnly();
    }

    public bool UpdateStatus(Guid id, MissionStatus status)
    {
        lock (_lock)
        {
            var mission = _missions.FirstOrDefault(m => m.Id == id);
            if (mission is null) return false;
            mission.Status = status;
            if (status == MissionStatus.Completed || status == MissionStatus.Failed)
                mission.CompletedAt = DateTime.UtcNow;
            return true;
        }
    }
}
