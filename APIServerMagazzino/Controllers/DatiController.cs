using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Text.Json;

[ApiController]
[Route("dati")]
public class DatiController : ControllerBase
{
    private readonly string _connectionString =
        $"Data Source={Path.Combine(Directory.GetCurrentDirectory(), "Data", "Magazzino.db")}";

    // =========================
    // GET: /dati
    // =========================
    [HttpGet]
    public IActionResult GetAll()
    {
        var list = new List<Artifact>();

        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Artifact";

        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            list.Add(new Artifact
            {
                id = reader.GetInt32(0),
                name = reader.GetString(1),
                textDescription = reader.GetString(2),
                shelvingUnit = reader.GetInt32(3),
                lastShelvingUnit = reader.IsDBNull(4) ? -1 : reader.GetInt32(4),
                containerLocalPose = reader.IsDBNull(5) ? null : reader.GetString(5),
                artifactWidth = reader.IsDBNull(6) ? 0f : reader.GetFloat(6),
                artifactHeight = reader.IsDBNull(7) ? 0f : reader.GetFloat(7),
                artifactDepth = reader.IsDBNull(8) ? 0f : reader.GetFloat(8)
            });
        }

        return Ok(list);
    }

    // =========================
    // GET: /dati/{id}
    // =========================
    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Artifact WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id);

        using var reader = cmd.ExecuteReader();

        if (!reader.Read())
        {
            return NotFound(new { message = "Artifact non trovato" });
        }

        var artifact = new Artifact
        {
            id = reader.GetInt32(0),
            name = reader.GetString(1),
            textDescription = reader.GetString(2),
            shelvingUnit = reader.GetInt32(3),
            lastShelvingUnit = reader.GetInt32(4),
            containerLocalPose = reader.IsDBNull(5) ? null : reader.GetString(5),
            artifactWidth = reader.IsDBNull(6) ? 0f : reader.GetFloat(6),
            artifactHeight = reader.IsDBNull(7) ? 0f : reader.GetFloat(7),
            artifactDepth = reader.IsDBNull(8) ? 0f : reader.GetFloat(8)
        };

        return Ok(artifact);
    }

    // =========================
    // POST: /dati
    // =========================
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ArtifactCreate dto)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        var cmd = conn.CreateCommand();

        cmd.CommandText = @"
            INSERT INTO Artifact (Name, TextDescription, ShelvingUnit, LastShelvingUnit, ContainerLocalPose, ArtifactWidth, ArtifactHeight, ArtifactDepth)
            VALUES ($name, $desc, $unit, $last, $pose, $width, $height, $depth);
            SELECT last_insert_rowid();
            ";

        cmd.Parameters.AddWithValue("$name", dto.name);
        cmd.Parameters.AddWithValue("$desc", dto.textDescription);
        cmd.Parameters.AddWithValue("$unit", dto.shelvingUnit);
        cmd.Parameters.AddWithValue("$last", dto.lastShelvingUnit);
        cmd.Parameters.AddWithValue("$pose", dto.containerLocalPose);
        cmd.Parameters.AddWithValue("$width", dto.artifactWidth);
        cmd.Parameters.AddWithValue("$height", dto.artifactHeight);
        cmd.Parameters.AddWithValue("$depth", dto.artifactDepth);

        var id = (long)cmd.ExecuteScalar();

        var created = new
        {
            eventType = "create",
            entityType = "artifact",
            id = id,
            name = dto.name,
            textDescription = dto.textDescription,
            shelvingUnit = dto.shelvingUnit,
            lastShelvingUnit = dto.lastShelvingUnit,
            containerLocalPose = dto.containerLocalPose,
            artifactWidth = dto.artifactWidth,
            artifactHeight = dto.artifactHeight,
            artifactDepth = dto.artifactDepth
        };

        await WebSocketManager.Broadcast(JsonSerializer.Serialize(created));

        return Ok(created);
    }

    // =========================
    // DELETE: /dati/{id}
    // =========================
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        var cmd = conn.CreateCommand();

        cmd.CommandText =
            "DELETE FROM Artifact WHERE Id = $id";

        cmd.Parameters.AddWithValue("$id", id);

        int rows = cmd.ExecuteNonQuery();

        if (rows == 0)
            return NotFound();

        //websocket message
        var deleted = new
        {
            eventType = "delete",
            id = id
        };

        await WebSocketManager.Broadcast(JsonSerializer.Serialize(deleted));

        return Ok(deleted);
    }

    // =========================
    // UPDATE: /dati/{id}
    // =========================
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] ArtifactCreate dto)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE Artifact
            SET Name = $name,
                TextDescription = $desc,
                ShelvingUnit = $unit,
                LastShelvingUnit = $last,
                ContainerLocalPose = $pose,
                ArtifactWidth = $width,
                ArtifactHeight = $height,
                ArtifactDepth = $depth
            WHERE Id = $id
            ";

        cmd.Parameters.AddWithValue("$name", dto.name);
        cmd.Parameters.AddWithValue("$desc", dto.textDescription);
        cmd.Parameters.AddWithValue("$unit", dto.shelvingUnit);
        cmd.Parameters.AddWithValue("$last", dto.lastShelvingUnit);
        cmd.Parameters.AddWithValue("$pose", dto.containerLocalPose);
        cmd.Parameters.AddWithValue("$width", dto.artifactWidth);
        cmd.Parameters.AddWithValue("$height", dto.artifactHeight);
        cmd.Parameters.AddWithValue("$depth", dto.artifactDepth);
        cmd.Parameters.AddWithValue("$id", id);

        int rows = cmd.ExecuteNonQuery();

        if (rows == 0)
            return NotFound();

        var updated = new
        {
            eventType = "update",
            entityType = "artifact",
            id = id,
            name = dto.name,
            textDescription = dto.textDescription,
            shelvingUnit = dto.shelvingUnit,
            lastShelvingUnit = dto.lastShelvingUnit,
            containerLocalPose = dto.containerLocalPose,
            artifactWidth = dto.artifactWidth,
            artifactHeight = dto.artifactHeight,
            artifactDepth = dto.artifactDepth
        };

        await WebSocketManager.Broadcast(JsonSerializer.Serialize(updated));

        return Ok(updated);
    }

    // =========================
    // GET: /dati/container/{containerId}
    // =========================
    [HttpGet("container/{containerId}")]
    public IActionResult GetArtifactsByContainer(int containerId)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        // Recupera tutti gli scaffali
        var shelves = new List<(int id, int parentId)>();

        var shelfCmd = conn.CreateCommand();
        shelfCmd.CommandText =
            "SELECT Id, ParentShelfId FROM Shelf";

        using (var reader = shelfCmd.ExecuteReader())
        {
            while (reader.Read())
            {
                shelves.Add((
                    reader.GetInt32(0),
                    reader.IsDBNull(1) ? -1 : reader.GetInt32(1)
                ));
            }
        }

        // Costruisce l'insieme degli id di tutti gli scaffali
        // discendenti (compreso quello richiesto)
        var containerIds = new HashSet<int>();

        void Visit(int id)
        {
            if (!containerIds.Add(id))
                return;

            foreach (var shelf in shelves)
            {
                if (shelf.parentId == id)
                    Visit(shelf.id);
            }
        }

        Visit(containerId);

        // Recupera gli artefatti
        var artifacts = new List<Artifact>();

        foreach (int id in containerIds)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText =
                "SELECT * FROM Artifact WHERE ShelvingUnit = $id";
            cmd.Parameters.AddWithValue("$id", id);

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                artifacts.Add(new Artifact
                {
                    id = reader.GetInt32(0),
                    name = reader.GetString(1),
                    textDescription = reader.GetString(2),
                    shelvingUnit = reader.GetInt32(3),
                    lastShelvingUnit = reader.IsDBNull(4) ? -1 : reader.GetInt32(4),
                    containerLocalPose = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    artifactWidth = reader.IsDBNull(6) ? 0f : reader.GetFloat(6),
                    artifactHeight = reader.IsDBNull(7) ? 0f : reader.GetFloat(7),
                    artifactDepth = reader.IsDBNull(8) ? 0f : reader.GetFloat(8)
                });
            }
        }

        return Ok(artifacts);
    }
}

public class Artifact
{
    public int id { get; set; }
    public string? name { get; set; }
    public string? textDescription { get; set; }
    public int shelvingUnit { get; set; }
    public int lastShelvingUnit { get; set; }
    public string? containerLocalPose { get; set; }
    public float artifactWidth { get; set; }
    public float artifactHeight { get; set; }
    public float artifactDepth { get; set; }
}

public class ArtifactCreate
{
    public string? name { get; set; }
    public string? textDescription { get; set; }
    public int shelvingUnit { get; set; }
    public int lastShelvingUnit { get; set; }
    public string? containerLocalPose { get; set; }
    public float artifactWidth { get; set; }
    public float artifactHeight { get; set; }
    public float artifactDepth { get; set; }
}