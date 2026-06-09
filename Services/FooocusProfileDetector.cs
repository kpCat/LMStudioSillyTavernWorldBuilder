using LMStudioSillyTavernWorldBuilder.Providers;

namespace LMStudioSillyTavernWorldBuilder.Services;

internal sealed class FooocusProfileDetector
{
    public FooocusSettings DetectFromFolder(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder))
        {
            return new FooocusSettings();
        }

        var selected = Path.GetFullPath(folder);
        var root = Directory.Exists(Path.Combine(selected, "Fooocus")) || File.Exists(Path.Combine(selected, "run.bat"))
            ? selected
            : Directory.GetParent(selected)?.FullName ?? selected;

        if (!File.Exists(Path.Combine(root, "run.bat")) && string.Equals(Path.GetFileName(selected), "Fooocus", StringComparison.OrdinalIgnoreCase))
        {
            root = Directory.GetParent(selected)?.FullName ?? selected;
        }

        var fooocusFolder = Path.Combine(root, "Fooocus");
        var output = DetectOutputDirectory(root, fooocusFolder);

        return new FooocusSettings
        {
            LaunchFilePath = File.Exists(Path.Combine(root, "run.bat")) ? Path.Combine(root, "run.bat") : "",
            WorkingDirectory = root,
            OutputDirectory = output,
            WebEndpoint = "",
            StartupTimeoutSeconds = 180,
            ShutdownTimeoutSeconds = 30
        };
    }

    public static string DetectOutputDirectory(string root, string fooocusFolder)
    {
        var configPath = Path.Combine(fooocusFolder, "config.txt");
        if (File.Exists(configPath))
        {
            foreach (var line in File.ReadLines(configPath))
            {
                var trimmed = line.Trim();
                var keyLine = trimmed.TrimStart('"', '\'');
                if (keyLine.StartsWith("path_outputs", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = keyLine.Split(new[] { '=', ':' }, 2);
                    if (parts.Length == 2)
                    {
                        var candidate = parts[1].Trim().TrimEnd(',').Trim().Trim('"', '\'');
                        if (!string.IsNullOrWhiteSpace(candidate))
                        {
                            var full = Path.IsPathRooted(candidate)
                                ? candidate
                                : Path.GetFullPath(Path.Combine(fooocusFolder, candidate));
                            if (Directory.Exists(full) || CanCreate(full))
                            {
                                return full;
                            }
                        }
                    }
                }
            }
        }

        return Path.Combine(root, "Fooocus", "outputs");
    }

    private static bool CanCreate(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
