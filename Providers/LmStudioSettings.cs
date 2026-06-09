namespace LMStudioSillyTavernWorldBuilder.Providers;

public sealed class LmStudioSettings
{
    public string Endpoint { get; set; } = "http://127.0.0.1:1234/v1";
    public string ApiKey { get; set; } = "lm-studio";
    public string ModelId { get; set; } = "";
    public string HealthCheckUrl { get; set; } = "";
    public string UnloadUrl { get; set; } = "";
    public string UnloadCommand { get; set; } = "";
    public int RequestTimeoutSeconds { get; set; } = 0;
    public int UnloadCommandTimeoutSeconds { get; set; } = 60;
    public bool ContinueIfUnloadFails { get; set; } = true;
}
