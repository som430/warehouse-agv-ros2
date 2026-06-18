using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

class AgvClient
{
    static async Task Main(string[] args)
    {
        var ws = new ClientWebSocket();
        await ws.ConnectAsync(new Uri("ws://localhost:9090"), CancellationToken.None);
        Console.WriteLine("rosbridge 연결 성공!");

        // /odom 구독
        await Send(ws, new {
            op = "subscribe",
            topic = "/odom",
            type = "nav_msgs/Odometry"
        });

        // 위치 수신 태스크
        var receiveTask = Task.Run(async () =>
        {
            var buffer = new byte[4096];
            while (ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(buffer, CancellationToken.None);
                var msg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                var doc = JsonDocument.Parse(msg);
                if (doc.RootElement.TryGetProperty("msg", out var data))
                {
                    var x = data.GetProperty("pose").GetProperty("pose")
                               .GetProperty("position").GetProperty("x").GetDouble();
                    var y = data.GetProperty("pose").GetProperty("pose")
                               .GetProperty("position").GetProperty("y").GetDouble();
                    Console.WriteLine($"현재 위치 → x: {x:F2}, y: {y:F2}");
                }
            }
        });

        // /cmd_vel 전송 (3초 전진 후 정지)
        Console.WriteLine("전진 명령 전송 중...");
        for (int i = 0; i < 6; i++)
        {
            await Send(ws, new {
                op = "publish",
                topic = "/cmd_vel",
                msg = new { linear = new { x = 0.2, y = 0.0, z = 0.0 },
                            angular = new { x = 0.0, y = 0.0, z = 0.0 } }
            });
            await Task.Delay(500);
        }

        // 정지
        await Send(ws, new {
            op = "publish",
            topic = "/cmd_vel",
            msg = new { linear = new { x = 0.0, y = 0.0, z = 0.0 },
                        angular = new { x = 0.0, y = 0.0, z = 0.0 } }
        });
        Console.WriteLine("정지 명령 전송");

        await Task.Delay(2000);
        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
    }

    static async Task Send(ClientWebSocket ws, object obj)
    {
        var json = JsonSerializer.Serialize(obj);
        var bytes = Encoding.UTF8.GetBytes(json);
        await ws.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
    }
}
