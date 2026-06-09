using System.Diagnostics;
using System.Text.Json;
using LMStudioSillyTavernWorldBuilder.Models;
using LMStudioSillyTavernWorldBuilder.Providers;

namespace LMStudioSillyTavernWorldBuilder.Services;

internal sealed class FooocusService
{
    private Process? _process;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task StartAsync(FooocusSettings settings, Action<string> log, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(settings.LaunchFilePath))
        {
            log("Fooocus launch file is not configured. Queue can still be exported for manual generation.");
            return;
        }

        if (!File.Exists(settings.LaunchFilePath))
        {
            log("Fooocus launch file was not found: " + settings.LaunchFilePath);
            return;
        }

        if (_process is { HasExited: false })
        {
            log("Fooocus is already running from this app.");
            return;
        }

        var startInfo = new ProcessStartInfo(settings.LaunchFilePath)
        {
            UseShellExecute = true,
            WorkingDirectory = string.IsNullOrWhiteSpace(settings.WorkingDirectory)
                ? Path.GetDirectoryName(settings.LaunchFilePath) ?? Environment.CurrentDirectory
                : settings.WorkingDirectory
        };

        _process = Process.Start(startInfo);
        log("Fooocus launch requested.");
        await Task.Delay(TimeSpan.FromSeconds(Math.Min(Math.Max(settings.StartupTimeoutSeconds, 1), 10)), cancellationToken);
    }

    public async Task StopAsync(FooocusSettings settings, Action<string> log, CancellationToken cancellationToken = default)
    {
        if (_process == null || _process.HasExited)
        {
            log("Fooocus process is not tracked as running.");
            return;
        }

        _process.CloseMainWindow();
        var timeout = TimeSpan.FromSeconds(Math.Max(settings.ShutdownTimeoutSeconds, 1));
        var completed = await Task.Run(() => _process.WaitForExit((int)timeout.TotalMilliseconds), cancellationToken);
        if (!completed && !_process.HasExited)
        {
            log("Fooocus did not exit before timeout; leaving it running.");
        }
        else
        {
            log("Fooocus stopped.");
        }
    }

    public void ExportQueue(GameProjectData project, Action<string> log)
    {
        if (string.IsNullOrWhiteSpace(project.Summary.ProjectPath))
        {
            log("Save project before exporting Fooocus queue.");
            return;
        }

        var queuePath = Path.Combine(project.Summary.ProjectPath, "prompts", "fooocus-queue.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(queuePath)!);
        var lines = new List<string>();
        foreach (var prompt in project.ImagePrompts.Where(x => x.Status is ImagePromptStatus.Approved or ImagePromptStatus.Queued or ImagePromptStatus.Draft))
        {
            lines.Add("==== " + prompt.Title + " ====");
            lines.Add("Positive: " + prompt.PositivePrompt);
            lines.Add("Negative: " + prompt.NegativePrompt);
            lines.Add($"Count: {prompt.Count}");
            lines.Add($"Size: {prompt.PreferredWidth}x{prompt.PreferredHeight}");
            lines.Add("");
            prompt.Status = ImagePromptStatus.Queued;
        }

        File.WriteAllLines(queuePath, lines);
        log("Fooocus queue exported: " + queuePath);
        ExportQueueJson(project, log);
        log("Автоматическая отправка prompt-ов в Fooocus API пока не включена. Очередь сохранена в fooocus-queue.txt; откройте Fooocus и сгенерируйте изображения вручную или подключите API adapter позже.");
    }

    public void ExportQueueJson(GameProjectData project, Action<string> log)
    {
        if (string.IsNullOrWhiteSpace(project.Summary.ProjectPath))
        {
            log("Save project before exporting Fooocus JSON queue.");
            return;
        }

        var queuePath = Path.Combine(project.Summary.ProjectPath, "prompts", "fooocus-queue.json");
        Directory.CreateDirectory(Path.GetDirectoryName(queuePath)!);
        var rows = project.ImagePrompts
            .Where(x => x.Status is ImagePromptStatus.Approved or ImagePromptStatus.Queued or ImagePromptStatus.Draft)
            .Select(x => new FooocusQueueItem
            {
                AssetId = x.AssetId,
                PositivePrompt = x.PositivePrompt,
                NegativePrompt = x.NegativePrompt,
                Count = x.Count,
                Width = x.PreferredWidth,
                Height = x.PreferredHeight,
                OutputFolder = x.OutputFolder,
                TargetType = x.TargetType,
                TargetEntityId = x.TargetEntityId
            })
            .ToList();
        File.WriteAllText(queuePath, JsonSerializer.Serialize(rows, _jsonOptions));
        log("Fooocus JSON queue exported: " + queuePath);
    }

    public int ImportGeneratedImages(GameProjectData project, FooocusSettings settings, Action<string> log)
    {
        var outputDirectory = settings.OutputDirectory;
        if (string.IsNullOrWhiteSpace(outputDirectory) || !Directory.Exists(outputDirectory))
        {
            log("Fooocus output directory is not configured or does not exist.");
            return 0;
        }

        var importFolderName = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var projectAssets = Path.Combine(project.Summary.ProjectPath, "assets", "generated-imports", importFolderName);
        Directory.CreateDirectory(projectAssets);
        var count = 0;
        foreach (var file in Directory.EnumerateFiles(outputDirectory, "*.*", SearchOption.TopDirectoryOnly)
            .Where(x => x.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || x.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || x.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) || x.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)))
        {
            var target = Path.Combine(projectAssets, Path.GetFileName(file));
            if (!File.Exists(target))
            {
                File.Copy(file, target);
                project.GeneratedImageCandidates.Add(new ImageGeneratedCandidate
                {
                    CandidateId = "candidate_" + Guid.NewGuid().ToString("N")[..12],
                    SourcePath = file,
                    ProjectRelativePath = Path.GetRelativePath(project.Summary.ProjectPath, target),
                    ImportedAtUtc = DateTime.UtcNow,
                    Notes = "Imported from Fooocus output staging."
                });
                count++;
            }
        }

        log($"Imported generated image candidates into staging: {count}");
        return count;
    }

    private sealed class FooocusQueueItem
    {
        public string AssetId { get; set; } = "";
        public string PositivePrompt { get; set; } = "";
        public string NegativePrompt { get; set; } = "";
        public int Count { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string OutputFolder { get; set; } = "";
        public ImageTargetType TargetType { get; set; }
        public string TargetEntityId { get; set; } = "";
    }
}
