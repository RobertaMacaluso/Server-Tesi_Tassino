using System.Net.WebSockets;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;

public static class WebSocketManager
{
    private static readonly List<WebSocket> sockets = new();

    public static void AddSocket(WebSocket socket)
    {
        sockets.Add(socket);

        Console.WriteLine("Client WebSocket connesso");
    }

    public static async Task Broadcast(string message)
    {
        var buffer = Encoding.UTF8.GetBytes(message);

        var segment = new ArraySegment<byte>(buffer);

        List<WebSocket> disconnected = new();

        foreach (var socket in sockets)
        {
            if (socket.State == WebSocketState.Open)
            {
                await socket.SendAsync(
                    segment,
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None
                );
            }
            else
            {
                disconnected.Add(socket);
            }
        }

        foreach (var d in disconnected)
        {
            sockets.Remove(d);
        }
    }
}