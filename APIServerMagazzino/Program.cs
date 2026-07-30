using Microsoft.Data.Sqlite;
using System.Net.WebSockets;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var app = builder.Build();

var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Magazzino.db");

// TEST DB
using (var conn = new SqliteConnection($"Data Source={dbPath}"))
{
    conn.Open();
}

app.UseRouting();

app.UseWebSockets();

app.Use(async (context, next) =>
{
    Console.WriteLine("WS HIT: " + context.Request.Path);

    if (context.Request.Path == "/ws")
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            var socket = await context.WebSockets.AcceptWebSocketAsync();

            WebSocketManager.AddSocket(socket);

            var buffer = new byte[1024];

            while (socket.State == WebSocketState.Open)
            {
                await socket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    CancellationToken.None
                );
            }
        }
        else
        {
            context.Response.StatusCode = 400;
        }
    }
    else
    {
        await next();
    }
});

app.MapControllers();

app.Run("http://0.0.0.0:5000");