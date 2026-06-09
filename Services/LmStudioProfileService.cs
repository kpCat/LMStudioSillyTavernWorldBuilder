using LMStudioSillyTavernWorldBuilder.Models;
using LMStudioSillyTavernWorldBuilder.Providers;

namespace LMStudioSillyTavernWorldBuilder.Services;

internal sealed class LmStudioProfileService
{
    public void NormalizeProfiles(AppSettings settings)
    {
        settings.LmStudio ??= new();
        settings.Generation ??= new();
        settings.LmStudioProfiles ??= new();

        if (settings.LmStudioProfiles.Count == 0)
        {
            settings.LmStudioProfiles.Add(new LmStudioModelProfile
            {
                Id = "default_local",
                Name = "Старый ПК / локальная LM Studio",
                Role = LmStudioProfileRole.Default,
                IsDefault = true,
                Settings = Clone(settings.LmStudio),
                Generation = Clone(settings.Generation)
            });
        }

        foreach (var profile in settings.LmStudioProfiles)
        {
            if (string.IsNullOrWhiteSpace(profile.Id))
            {
                profile.Id = "profile_" + Guid.NewGuid().ToString("N")[..8];
            }
            if (string.IsNullOrWhiteSpace(profile.Name))
            {
                profile.Name = "LM Studio профиль";
            }
            if (string.IsNullOrWhiteSpace(profile.Role))
            {
                profile.Role = LmStudioProfileRole.Default;
            }

            profile.Settings ??= new();
            profile.Generation ??= new();
            NormalizeGeneration(profile.Generation);
        }

        var defaults = settings.LmStudioProfiles.Where(x => x.IsDefault).ToList();
        if (defaults.Count == 0)
        {
            settings.LmStudioProfiles[0].IsDefault = true;
        }
        else
        {
            foreach (var profile in defaults.Skip(1))
            {
                profile.IsDefault = false;
            }
        }

        if (string.IsNullOrWhiteSpace(settings.ActiveLmStudioProfileId)
            || settings.LmStudioProfiles.All(x => !string.Equals(x.Id, settings.ActiveLmStudioProfileId, StringComparison.OrdinalIgnoreCase)))
        {
            settings.ActiveLmStudioProfileId = settings.LmStudioProfiles.First(x => x.IsDefault).Id;
        }

        var active = GetActiveProfile(settings);
        settings.LmStudio = Clone(active.Settings);
        settings.Generation = Clone(active.Generation);
    }

    public LmStudioModelProfile GetActiveProfile(AppSettings settings)
    {
        NormalizeProfilesIfNeeded(settings);
        return settings.LmStudioProfiles.FirstOrDefault(x => string.Equals(x.Id, settings.ActiveLmStudioProfileId, StringComparison.OrdinalIgnoreCase))
            ?? settings.LmStudioProfiles.FirstOrDefault(x => x.IsDefault)
            ?? settings.LmStudioProfiles[0];
    }

    public LmStudioModelProfile ResolveProfileForPurpose(AppSettings settings, string purpose)
    {
        NormalizeProfilesIfNeeded(settings);
        var active = GetActiveProfile(settings);
        if (!settings.AutoSelectLmStudioProfile)
        {
            return active;
        }

        foreach (var role in RolesForPurpose(purpose))
        {
            var profile = settings.LmStudioProfiles.FirstOrDefault(x => string.Equals(x.Role, role, StringComparison.OrdinalIgnoreCase));
            if (profile != null)
            {
                return profile;
            }
        }

        return settings.LmStudioProfiles.FirstOrDefault(x => x.IsDefault) ?? active;
    }

    public void DeleteProfile(AppSettings settings, string profileId)
    {
        NormalizeProfilesIfNeeded(settings);
        if (settings.LmStudioProfiles.Count <= 1)
        {
            var only = settings.LmStudioProfiles[0];
            only.IsDefault = true;
            settings.ActiveLmStudioProfileId = only.Id;
            return;
        }

        var index = settings.LmStudioProfiles.FindIndex(x => string.Equals(x.Id, profileId, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
        {
            settings.LmStudioProfiles.RemoveAt(index);
        }

        NormalizeProfiles(settings);
    }

    public void ApplyProfileToLegacySettings(AppSettings settings, LmStudioModelProfile profile)
    {
        settings.ActiveLmStudioProfileId = profile.Id;
        settings.LmStudio = Clone(profile.Settings);
        settings.Generation = Clone(profile.Generation);
    }

    public static LmStudioSettings Clone(LmStudioSettings source)
    {
        return new LmStudioSettings
        {
            Endpoint = source.Endpoint,
            ApiKey = source.ApiKey,
            ModelId = source.ModelId,
            HealthCheckUrl = source.HealthCheckUrl,
            UnloadUrl = source.UnloadUrl,
            UnloadCommand = source.UnloadCommand,
            RequestTimeoutSeconds = source.RequestTimeoutSeconds,
            UnloadCommandTimeoutSeconds = source.UnloadCommandTimeoutSeconds,
            ContinueIfUnloadFails = source.ContinueIfUnloadFails
        };
    }

    public static GenerationUiSettings Clone(GenerationUiSettings source)
    {
        return new GenerationUiSettings
        {
            Temperature = source.Temperature,
            TopP = source.TopP,
            MinP = source.MinP,
            TopK = source.TopK,
            RepeatPenalty = source.RepeatPenalty,
            PresencePenalty = source.PresencePenalty,
            MaxTokens = source.MaxTokens,
            MaxInputContextTokens = source.MaxInputContextTokens,
            MaxOutputTokens = source.MaxOutputTokens,
            ApproxCharsPerToken = source.ApproxCharsPerToken
        };
    }

    public static IReadOnlyList<string> RolesForPurpose(string purpose)
    {
        var normalized = (purpose ?? string.Empty).Trim().ToLowerInvariant();
        return normalized switch
        {
            "discussion" or "idea-chat" or "design-assumptions" => [LmStudioProfileRole.Discussion, LmStudioProfileRole.Default],
            "balance" or "balance-simulator" => [LmStudioProfileRole.Balance, LmStudioProfileRole.Review, LmStudioProfileRole.LargeContext, LmStudioProfileRole.JsonStrict, LmStudioProfileRole.Default],
            "review" or "revision-fix" or "change-request" or "mvp-orchestrator" or "random-director" or "initial-content" => [LmStudioProfileRole.Review, LmStudioProfileRole.LargeContext, LmStudioProfileRole.JsonStrict, LmStudioProfileRole.Default],
            "json-draft" or "json_strict" or "batch" or "image-prompts" => [LmStudioProfileRole.JsonStrict, LmStudioProfileRole.LargeContext, LmStudioProfileRole.Default],
            "creative" or "brief" or "concept" or "mvp" or "structure" => [LmStudioProfileRole.Creative, LmStudioProfileRole.LargeContext, LmStudioProfileRole.Default],
            _ => [LmStudioProfileRole.Default]
        };
    }

    public static string FormatProfileDisplayName(LmStudioModelProfile profile, bool isActive)
    {
        var markers = new List<string>();
        if (isActive) markers.Add("активный");
        if (profile.IsDefault) markers.Add("default");
        markers.Add(profile.Role);
        return $"{profile.Name} [{string.Join(", ", markers)}]";
    }

    private static void NormalizeProfilesIfNeeded(AppSettings settings)
    {
        if (settings.LmStudioProfiles == null || settings.LmStudioProfiles.Count == 0)
        {
            new LmStudioProfileService().NormalizeProfiles(settings);
        }
    }

    private static void NormalizeGeneration(GenerationUiSettings generation)
    {
        if (generation.MaxInputContextTokens <= 0) generation.MaxInputContextTokens = 32768;
        if (generation.MaxOutputTokens <= 0) generation.MaxOutputTokens = generation.MaxTokens > 0 ? generation.MaxTokens : 4096;
        if (generation.MaxTokens <= 0) generation.MaxTokens = generation.MaxOutputTokens;
        if (generation.ApproxCharsPerToken <= 0) generation.ApproxCharsPerToken = 4;
    }
}
