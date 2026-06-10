using System.Net;
using LMStudioSillyTavernWorldBuilder.Providers;
using LMStudioSillyTavernWorldBuilder.Services;

namespace LMStudioSillyTavernWorldBuilder.Tests;

public sealed class LmStudioServiceTests
{
    [Fact]
    public void LmStudioSettings_DefaultTimeout_IsZero()
    {
        Assert.Equal(0, new LmStudioSettings().RequestTimeoutSeconds);
    }

    [Fact]
    public async Task LmStudioService_ZeroTimeoutMeansInfinite()
    {
        var handler = new StubHandler();
        using var client = new HttpClient(handler);
        var service = new LmStudioService(client);

        await service.SendAsync(new LmStudioSettings { RequestTimeoutSeconds = 0, ModelId = "test" },
            new[] { new ApiMessage("user", "hello") },
            new GenerationSettings(0, 1, 0, 1, 1, 0, 1));

        Assert.Equal(Timeout.InfiniteTimeSpan, client.Timeout);
    }

    [Fact]
    public async Task LmStudioService_CanSendMultipleRequestsWithDifferentTimeoutSettings()
    {
        var handler = new StubHandler();
        using var client = new HttpClient(handler);
        var service = new LmStudioService(client);

        var generation = new GenerationSettings(0, 1, 0, 1, 1, 0, 1);
        var messages = new[] { new ApiMessage("user", "hello") };

        var first = await service.SendAsync(new LmStudioSettings { RequestTimeoutSeconds = 0, ModelId = "test" }, messages, generation);
        var second = await service.SendAsync(new LmStudioSettings { RequestTimeoutSeconds = 30, ModelId = "test" }, messages, generation);

        Assert.Equal("ok", first);
        Assert.Equal("ok", second);
        Assert.Equal(Timeout.InfiniteTimeSpan, client.Timeout);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"choices":[{"message":{"content":"ok"}}]}""")
            });
        }
    }
}
