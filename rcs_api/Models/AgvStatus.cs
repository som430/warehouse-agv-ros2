namespace rcs_api.Models;

public class AgvStatus
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Yaw { get; set; }
    public bool IsMoving { get; set; }
    public Guid? CurrentMissionId { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
