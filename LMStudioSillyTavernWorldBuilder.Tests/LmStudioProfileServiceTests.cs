using LMStudioSillyTavernWorldBuilder.Models;
using LMStudioSillyTavernWorldBuilder.Providers;
using LMStudioSillyTavernWorldBuilder.Services;

namespace LMStudioSillyTavernWorldBuilder.Tests;

public sealed class LmStudioProfileServiceTests
{
    [Fact]
    public void NormalizeProfiles_MigratesOldSingleSettingsIntoDefaultProfile()
    {
        var settings = new AppSettings
        {
            LmStudio = new LmStudioSettings
            {
                Endpoint = "http://old-pc:1234/v1",
                ApiKey = "key",
                ModelId = "old-model"
            },
            Generation = new GenerationUiSettings
            {
                MaxInputContextTokens = 30000,
                MaxOutputTokens = 2048,
                MaxTokens = 2048,
                ApproxCharsPerToken = 5
            }
        };

        new LmStudioProfileService().NormalizeProfiles(settings);

        var profile = Assert.Single(settings.LmStudioProfiles);
        Assert.True(profile.IsDefault);
        Assert.Equal(LmStudioProfileRole.Default, profile.Role);
        Assert.Equal("http://old-pc:1234/v1", profile.Settings.Endpoint);
        Assert.Equal("old-model", profile.Settings.ModelId);
        Assert.Equal(30000, profile.Generation.MaxInputContextTokens);
        Assert.Equal(profile.Id, settings.ActiveLmStudioProfileId);
    }

    [Fact]
    public void ResolveProfileForPurpose_ReturnsActiveWhenAutoSelectIsFalse()
    {
        var settings = CreateSettingsWithProfiles(autoSelect: false);
        settings.ActiveLmStudioProfileId = "discussion";

        var profile = new LmStudioProfileService().ResolveProfileForPurpose(settings, "balance");

        Assert.Equal("discussion", profile.Id);
    }

    [Fact]
    public void ResolveProfileForPurpose_SelectsRoleWhenAutoSelectIsTrue()
    {
        var settings = CreateSettingsWithProfiles(autoSelect: true);

        var jsonProfile = new LmStudioProfileService().ResolveProfileForPurpose(settings, "json-draft");
        var balanceProfile = new LmStudioProfileService().ResolveProfileForPurpose(settings, "balance");
        var discussionProfile = new LmStudioProfileService().ResolveProfileForPurpose(settings, "discussion");

        Assert.Equal("json", jsonProfile.Id);
        Assert.Equal("balance", balanceProfile.Id);
        Assert.Equal("discussion", discussionProfile.Id);
    }

    [Fact]
    public void ResolveProfileForPurpose_FallsBackSafelyWhenRoleIsMissing()
    {
        var settings = new AppSettings
        {
            AutoSelectLmStudioProfile = true,
            LmStudioProfiles =
            {
                new LmStudioModelProfile
                {
                    Id = "default",
                    Name = "Default",
                    Role = LmStudioProfileRole.Default,
                    IsDefault = true,
                    Settings = new LmStudioSettings { ModelId = "default-model" },
                    Generation = new GenerationUiSettings()
                }
            },
            ActiveLmStudioProfileId = "default"
        };

        var profile = new LmStudioProfileService().ResolveProfileForPurpose(settings, "balance");

        Assert.Equal("default", profile.Id);
    }

    [Fact]
    public void ApplyProfileToLegacySettings_TransfersGenerationLimits()
    {
        var settings = CreateSettingsWithProfiles(autoSelect: true);
        var profile = settings.LmStudioProfiles.Single(x => x.Id == "large");

        new LmStudioProfileService().ApplyProfileToLegacySettings(settings, profile);

        Assert.Equal("large", settings.ActiveLmStudioProfileId);
        Assert.Equal("large-model", settings.LmStudio.ModelId);
        Assert.Equal(96000, settings.Generation.MaxInputContextTokens);
        Assert.Equal(8192, settings.Generation.MaxOutputTokens);
        Assert.Equal(3, settings.Generation.ApproxCharsPerToken);
    }

    [Fact]
    public void DeleteProfile_NormalizesWithoutZeroUsableProfiles()
    {
        var settings = CreateSettingsWithProfiles(autoSelect: true);
        var service = new LmStudioProfileService();

        service.DeleteProfile(settings, "default");
        service.DeleteProfile(settings, "json");
        service.DeleteProfile(settings, "discussion");
        service.DeleteProfile(settings, "balance");
        service.DeleteProfile(settings, "large");

        Assert.NotEmpty(settings.LmStudioProfiles);
        Assert.Contains(settings.LmStudioProfiles, x => x.IsDefault);
        Assert.False(string.IsNullOrWhiteSpace(settings.ActiveLmStudioProfileId));
    }

    private static AppSettings CreateSettingsWithProfiles(bool autoSelect)
    {
        return new AppSettings
        {
            AutoSelectLmStudioProfile = autoSelect,
            ActiveLmStudioProfileId = "default",
            LmStudioProfiles =
            {
                new LmStudioModelProfile
                {
                    Id = "default",
                    Name = "Default",
                    Role = LmStudioProfileRole.Default,
                    IsDefault = true,
                    Settings = new LmStudioSettings { ModelId = "default-model" },
                    Generation = new GenerationUiSettings { MaxInputContextTokens = 30000, MaxOutputTokens = 4096, MaxTokens = 4096, ApproxCharsPerToken = 4 }
                },
                new LmStudioModelProfile
                {
                    Id = "discussion",
                    Name = "Discussion",
                    Role = LmStudioProfileRole.Discussion,
                    Settings = new LmStudioSettings { ModelId = "discussion-model" },
                    Generation = new GenerationUiSettings { MaxInputContextTokens = 32000, MaxOutputTokens = 2048, MaxTokens = 2048, ApproxCharsPerToken = 4 }
                },
                new LmStudioModelProfile
                {
                    Id = "json",
                    Name = "Json",
                    Role = LmStudioProfileRole.JsonStrict,
                    Settings = new LmStudioSettings { ModelId = "json-model" },
                    Generation = new GenerationUiSettings { MaxInputContextTokens = 48000, MaxOutputTokens = 4096, MaxTokens = 4096, ApproxCharsPerToken = 4 }
                },
                new LmStudioModelProfile
                {
                    Id = "large",
                    Name = "Large",
                    Role = LmStudioProfileRole.LargeContext,
                    Settings = new LmStudioSettings { ModelId = "large-model" },
                    Generation = new GenerationUiSettings { MaxInputContextTokens = 96000, MaxOutputTokens = 8192, MaxTokens = 8192, ApproxCharsPerToken = 3 }
                },
                new LmStudioModelProfile
                {
                    Id = "balance",
                    Name = "Balance",
                    Role = LmStudioProfileRole.Balance,
                    Settings = new LmStudioSettings { ModelId = "balance-model" },
                    Generation = new GenerationUiSettings { MaxInputContextTokens = 64000, MaxOutputTokens = 4096, MaxTokens = 4096, ApproxCharsPerToken = 4 }
                }
            }
        };
    }
}
