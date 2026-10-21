using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using System.IO;

[ApiController]
[Route("experiment")]
public class ExperimentController : ControllerBase
{
    private readonly string connectionString;

    public ExperimentController(IWebHostEnvironment environment)
    {
        string databasePath = Path.Combine(
            environment.ContentRootPath, "Data", "Magazzino.db");

        connectionString = $"Data Source={databasePath}";
    }

    [HttpGet("settings")]
    public IActionResult GetSettings()
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, ParticipantId, Condition, Instance, TaskNumber, TaskText
            FROM ExperimentSettings
            WHERE Id = 1;";

        using var reader = command.ExecuteReader();

        if (!reader.Read())
        {
            return NotFound(new
            {
                message = "Configurazione sperimentale non presente."
            });
        }

        // Nomi espliciti: corrispondono ai campi letti da JsonUtility in Unity.
        return Ok(new
        {
            id = reader.GetInt32(0),
            participantId = reader.GetString(1),
            condition = reader.GetString(2),
            instance = reader.GetString(3),
            taskNumber = reader.GetInt32(4),
            taskText = reader.GetString(5)
        });
    }
}