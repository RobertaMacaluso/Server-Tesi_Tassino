using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using System.Text.Json;

[ApiController]
[Route("shelf")]
public class ShelfController : ControllerBase
{
    private readonly string _connectionString =
        $"Data Source={Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Magazzino.db")}";

    // =========================
    // GET: /shelf
    // =========================
    [HttpGet]
    public IActionResult GetAll()
    {
        var list = new List<Shelf>();

        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Shelf";

        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            list.Add(new Shelf
            {
                id = reader.GetInt32(0),
                name = reader.IsDBNull(1) ? "" : reader.GetString(1),
                worldTransform = reader.IsDBNull(2) ? "" : reader.GetString(2),

                parentShelfId = reader.IsDBNull(3) ? -1 : reader.GetInt32(3),

                isRoom = reader.GetBoolean(4),

                markerId = reader.IsDBNull(5) ? -1 : reader.GetInt32(5),

                roomWidth = reader.IsDBNull(6) ? 0f : reader.GetFloat(6),
                roomHeight = reader.IsDBNull(7) ? 0f : reader.GetFloat(7),
                roomDepth = reader.IsDBNull(8) ? 0f : reader.GetFloat(8),

                roomCenterPose = reader.IsDBNull(9) ? "" : reader.GetString(9),
            });
        }

        return Ok(list);
    }

    // =========================
    // POST: /shelf
    // =========================
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ShelfCreate dto)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        var cmd = conn.CreateCommand();

        cmd.CommandText = @"
            INSERT INTO Shelf
            (Name, Pose, ParentShelfId, IsRoom, MarkerId, RoomWidth, RoomHeight, RoomDepth, RoomCenterPose)
            VALUES
            ($name, $pose, $parent, $isRoom, $markerId, $w, $h, $d, $cPose);

            SELECT last_insert_rowid();
            ";

        cmd.Parameters.AddWithValue("$name", dto.name);
        cmd.Parameters.AddWithValue("$pose", dto.worldTransform ?? "");

        cmd.Parameters.AddWithValue("$parent",
            dto.parentShelfId == -1 ? DBNull.Value : dto.parentShelfId);

        cmd.Parameters.AddWithValue("$isRoom", dto.isRoom ? 1 : 0);

        cmd.Parameters.AddWithValue("$markerId", dto.markerId);

        cmd.Parameters.AddWithValue("$w", dto.roomWidth);
        cmd.Parameters.AddWithValue("$h", dto.roomHeight);
        cmd.Parameters.AddWithValue("$d", dto.roomDepth);

        cmd.Parameters.AddWithValue("$cPose", dto.roomCenterPose ?? "");

        var id = (long)cmd.ExecuteScalar();

        // WEBSOCKET MESSAGE
        var created = new
        {
            eventType = "create",
            entityType = "shelf",
            id = id,
            name = dto.name,
            worldTransform = dto.worldTransform,
            parentShelfId = dto.parentShelfId,
            isRoom = dto.isRoom,
            markerId = dto.markerId,
            roomWidth = dto.roomWidth,
            roomHeight = dto.roomHeight,
            roomDepth = dto.roomDepth,
            roomCenterPose = dto.roomCenterPose
        };

        await WebSocketManager.Broadcast(
            JsonSerializer.Serialize(created)
        );

        return Ok(created);
    }


    // =========================
    // UPDATE: /shelf/{id}
    // =========================
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] ShelfCreate dto)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE Shelf
            SET Name = $name,
                Pose = $pose,
                ParentShelfId = $parent,
                IsRoom = $isRoom,
                MarkerId = $markerId,
                RoomWidth = $w,
                RoomHeight = $h,
                RoomDepth = $d,
                RoomCenterPose = $cPose
            WHERE Id = $id
            ";

        cmd.Parameters.AddWithValue("$name", dto.name);
        cmd.Parameters.AddWithValue("$pose", dto.worldTransform ?? "");

        cmd.Parameters.AddWithValue("$parent",
            dto.parentShelfId == -1 ? DBNull.Value : dto.parentShelfId);

        cmd.Parameters.AddWithValue("$isRoom", dto.isRoom ? 1 : 0);

        cmd.Parameters.AddWithValue("$markerId", dto.markerId);

        cmd.Parameters.AddWithValue("$w", dto.roomWidth);
        cmd.Parameters.AddWithValue("$h", dto.roomHeight);
        cmd.Parameters.AddWithValue("$d", dto.roomDepth);

        cmd.Parameters.AddWithValue("$cPose", dto.roomCenterPose ?? "");

        cmd.Parameters.AddWithValue("$id", id);

        int rows = cmd.ExecuteNonQuery();

        if (rows == 0)
            return NotFound();

        var updated = new
        {
            eventType = "update",
            entityType = "shelf",
            id = id,
            name = dto.name,
            worldTransform = dto.worldTransform,
            parentShelfId = dto.parentShelfId,
            isRoom = dto.isRoom,
            markerId = dto.markerId,
            roomWidth = dto.roomWidth,
            roomHeight = dto.roomHeight,
            roomDepth = dto.roomDepth,
            roomCenterPose = dto.roomCenterPose
    
        };

        await WebSocketManager.Broadcast(JsonSerializer.Serialize(updated));

        return Ok(updated);
    }
}

public class Shelf
{
    public int id { get; set; }
    public string? name { get; set; }
    public string? worldTransform { get; set; }
    public int parentShelfId { get; set; }
    public bool isRoom { get; set; }
    public int markerId { get; set; }
    public float roomWidth { get; set; }
    public float roomHeight { get; set; }
    public float roomDepth { get; set; }
    public string? roomCenterPose { get; set; }
}

public class ShelfCreate
{
    public string? name { get; set; }
    public string? worldTransform { get; set; }
    public int parentShelfId { get; set; }
    public bool isRoom { get; set; }
    public int markerId { get; set; }
    public float roomWidth { get; set; }
    public float roomHeight { get; set; }
    public float roomDepth { get; set; }
    public string? roomCenterPose { get; set; }
}