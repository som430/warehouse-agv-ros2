namespace rcs_api.Models;

public enum MissionStatus { Pending, Running, Completed, Failed }

public class Mission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TargetLocation { get; set; } = string.Empty;  // "x,y" 형식
    public MissionStatus Status { get; set; } = MissionStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}
