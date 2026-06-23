using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using rcs_api.Models;

namespace rcs_api.Services;

public class RosbridgeClientService : BackgroundService
{
    private readonly ILogger<RosbridgeClientService> _logger;
    private readonly AgvStatus _status = new();
    private ClientWebSocket _ws = new();
    private const string RosbridgeUrl = "ws://localhost:9090";

    public RosbridgeClientService(ILogger<RosbridgeClientService> logger)
        => _logger = logger;

    public AgvStatus GetStatus() => _status;
    public bool IsConnected => _ws.State == WebSocketState.Open;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                _ws = new ClientWebSocket();
                await _ws.ConnectAsync(new Uri(RosbridgeUrl), ct);
                _logger.LogInformation("Rosbridge connected");

                var subscribe = JsonSerializer.Serialize(new
                {
                    op = "subscribe",
                    topic = "/odom",
                    type = "nav_msgs/Odometry"
                });
                await SendAsync(subscribe, ct);
                await ReceiveLoopAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Rosbridge disconnected: {msg}. Retrying in 3s...", ex.Message);
                await Task.Delay(3000, ct);
            }
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        var buffer = new byte[4096];
        while (_ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
        {
            var result = await _ws.ReceiveAsync(buffer, ct);
            if (result.MessageType == WebSocketMessageType.Close) break;
            var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
            ParseOdom(json);
        }
    }

    private void ParseOdom(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var pos = doc.RootElement
                .GetProperty("msg")
                .GetProperty("pose")
                .GetProperty("pose")
                .GetProperty("position");

            _status.X = pos.GetProperty("x").GetDouble();
            _status.Y = pos.GetProperty("y").GetDouble();
            _status.LastUpdated = DateTime.UtcNow;
        }
        catch { }
    }

    public async Task PublishGoalAsync(string targetLocation, CancellationToken ct = default)
    {
        if (!IsConnected)
        {
            _logger.LogWarning("Rosbridge not connected. Goal not published.");
            return;
        }

        var parts = targetLocation.Split(',');
        if (parts.Length != 2) return;

        var x = double.Parse(parts[0]);
        var y = double.Parse(parts[1]);

        var msg = JsonSerializer.Serialize(new
        {
            op = "publish",
            topic = "/move_base_simple/goal",
            msg = new
            {
                header = new { frame_id = "map" },
                pose = new
                {
                    position = new { x, y, z = 0.0 },
                    orientation = new { x = 0.0, y = 0.0, z = 0.0, w = 1.0 }
                }
            }
        });

        await SendAsync(msg, ct);
        _status.IsMoving = true;
    }

    private async Task SendAsync(string message, CancellationToken ct)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        await _ws.SendAsync(bytes, WebSocketMessageType.Text, true, ct);
    }
}
