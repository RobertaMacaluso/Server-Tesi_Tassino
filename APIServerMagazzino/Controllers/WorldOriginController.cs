using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using System.Text.Json;

[ApiController]
[Route("api/[controller]")]
public class WorldOriginController : ControllerBase
{
    //private readonly string _connectionString;

    //public WorldOriginController(IConfiguration config)
    //{
    //    _connectionString = config.GetConnectionString("Default");
    //}
    private readonly string _connectionString =
        $"Data Source={Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Magazzino.db")}";

    // =========================
    // GET WORLD ORIGIN
    // =========================
    [HttpGet]
    public IActionResult Get()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT MarkerId, Position, Rotation, Timestamp
            FROM WorldOrigin
            WHERE Id = 1
        ";

        using var reader = cmd.ExecuteReader();

        if (!reader.Read())
            return NotFound();

        var result = new
        {
            markerId = reader.GetString(0),
            position = reader.GetString(1),
            rotation = reader.GetString(2),
            timestamp = reader.GetString(3)
        };

        return Ok(result);
    }

    // =========================
    // SET / UPDATE WORLD ORIGIN
    // =========================
    [HttpPost]
    public async Task<IActionResult> Set([FromBody] WorldOriginDto dto)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO WorldOrigin
            (Id, MarkerId, Position, Rotation, Timestamp)
            VALUES
            (1, $markerId, $position, $rotation, $timestamp)
        ";

        cmd.Parameters.AddWithValue("$markerId", dto.markerId);
        cmd.Parameters.AddWithValue("$position", dto.position);
        cmd.Parameters.AddWithValue("$rotation", dto.rotation);
        cmd.Parameters.AddWithValue("$timestamp", DateTime.UtcNow.ToString("O"));

        cmd.ExecuteNonQuery();

        var msg = new
        {
            eventType = "worldorigin_update",
            markerId = dto.markerId,
            position = dto.position,
            rotation = dto.rotation,
            timestamp = DateTime.UtcNow.ToString("O")
        };

        await WebSocketManager.Broadcast(JsonSerializer.Serialize(msg));

        return Ok(msg);
    }
}

public class WorldOriginDto
{
    public string markerId { get; set; }
    public string position { get; set; }   // "x_y_z"
    public string rotation { get; set; }   // "x_y_z_w"
}