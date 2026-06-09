using LMStudioSillyTavernWorldBuilder.Models;
using LMStudioSillyTavernWorldBuilder.Providers;

namespace LMStudioSillyTavernWorldBuilder.Services;

internal sealed class ExternalToolOrchestratorService
{
    private readonly LmStudioService _lmStudioService;
    private readonly FooocusService _fooocusService;

    public ExternalToolOrchestratorService(LmStudioService lmStudioService, FooocusService fooocusService)
    {
        _lmStudioService = lmStudioService;
        _fooocusService = fooocusService;
    }

    public async Task PrepareAndRunAssetQueueAsync(GameProjectData project, LmStudioSettings lmSettings, FooocusSettings fooocusSettings, Action<string> log, CancellationToken cancellationToken = default)
    {
        log("Preparing switch from LM Studio to Fooocus.");
        await _lmStudioService.TryUnloadAsync(lmSettings, log, cancellationToken);
        _fooocusService.ExportQueue(project, log);
        await _fooocusService.StartAsync(fooocusSettings, log, cancellationToken);
        log("Fooocus queue is ready. Generate images manually in Fooocus, then import output images from the Assets tab.");
    }
}
