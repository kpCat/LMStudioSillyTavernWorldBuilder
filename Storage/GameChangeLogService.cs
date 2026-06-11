using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using LMStudioSillyTavernWorldBuilder.Models;

namespace LMStudioSillyTavernWorldBuilder.Storage;

internal sealed class GameChangeLogService
{
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public async Task AppendChangeAsync(GameProjectData project, GameChangeRecord record, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(project.Summary.ProjectPath))
        {
            return;
        }

        Directory.CreateDirectory(Path.Combine(project.Summary.ProjectPath, "changes"));
        record.OperationId = string.IsNullOrWhiteSpace(record.OperationId) ? Ids.New("change") : record.OperationId;
        record.CreatedAtUtc = record.CreatedAtUtc == default ? DateTime.UtcNow : record.CreatedAtUtc;

        var sequence = Directory.EnumerateFiles(Path.Combine(project.Summary.ProjectPath, "changes"), "*.json").Count() + 1;
        var operation = GameProjectManifestService.SafeId(record.Operation, "operation");
        var entityType = GameProjectManifestService.SafeId(record.EntityType, "project");
        var entityId = GameProjectManifestService.SafeId(record.EntityId, "all");
        var fileName = $"{sequence:000000}_{operation}_{entityType}_{entityId}.json";
        var path = Path.Combine(project.Summary.ProjectPath, "changes", fileName);
        var json = JsonSerializer.Serialize(record, _jsonOptions);
        await File.WriteAllTextAsync(path, json, new UTF8Encoding(false), cancellationToken);
    }

    public IReadOnlyList<GameChangeRecord> LoadRecentChanges(GameProjectData project, int count)
    {
        if (string.IsNullOrWhiteSpace(project.Summary.ProjectPath))
        {
            return Array.Empty<GameChangeRecord>();
        }

        var folder = Path.Combine(project.Summary.ProjectPath, "changes");
        if (!Directory.Exists(folder))
        {
            return Array.Empty<GameChangeRecord>();
        }

        return Directory.EnumerateFiles(folder, "*.json")
            .OrderByDescending(Path.GetFileName)
            .Take(Math.Max(count, 0))
            .Select(ReadChange)
            .Where(x => x != null)
            .Cast<GameChangeRecord>()
            .ToList();
    }

    private GameChangeRecord? ReadChange(string path)
    {
        try
        {
            return JsonSerializer.Deserialize<GameChangeRecord>(File.ReadAllText(path), _jsonOptions);
        }
        catch
        {
            return null;
        }
    }
}
