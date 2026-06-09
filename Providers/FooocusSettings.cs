namespace LMStudioSillyTavernWorldBuilder.Providers;

public sealed class FooocusSettings
{
    public string LaunchFilePath { get; set; } = "";
    public string WorkingDirectory { get; set; } = "";
    public string OutputDirectory { get; set; } = "";
    public string WebEndpoint { get; set; } = "";
    public int StartupTimeoutSeconds { get; set; } = 180;
    public int ShutdownTimeoutSeconds { get; set; } = 30;
    public bool StopAfterQueue { get; set; } = false;
}
