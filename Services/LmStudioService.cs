using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using LMStudioSillyTavernWorldBuilder.Providers;

namespace LMStudioSillyTavernWorldBuilder.Services;

internal sealed class LmStudioService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false
    };

    public LmStudioService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = Timeout.InfiniteTimeSpan;
    }

    public async Task<string> SendAsync(
        LmStudioSettings lmSettings,
        IReadOnlyList<ApiMessage> messages,
        GenerationSettings generationSettings,
        CancellationToken cancellationToken = default)
    {
        var requestTimeout = lmSettings.RequestTimeoutSeconds <= 0
            ? Timeout.InfiniteTimeSpan
            : TimeSpan.FromSeconds(Math.Max(10, lmSettings.RequestTimeoutSeconds));

        var endpoint = BuildChatCompletionsUrl(lmSettings.Endpoint);
        var request = new ChatCompletionRequest
        {
            Model = lmSettings.ModelId.Trim(),
            Messages = messages.ToList(),
            Temperature = generationSettings.Temperature,
            TopP = generationSettings.TopP,
            MinP = generationSettings.MinP,
            TopK = generationSettings.TopK,
            RepeatPenalty = generationSettings.RepeatPenalty,
            PresencePenalty = generationSettings.PresencePenalty,
            MaxTokens = generationSettings.MaxTokens,
            Stream = false
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
        httpRequest.Content = new StringContent(JsonSerializer.Serialize(request, _jsonOptions), Encoding.UTF8, "application/json");

        if (!string.IsNullOrWhiteSpace(lmSettings.ApiKey))
        {
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", lmSettings.ApiKey.Trim());
        }

        using var timeoutCts = requestTimeout == Timeout.InfiniteTimeSpan
            ? null
            : CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (timeoutCts != null)
        {
            timeoutCts.CancelAfter(requestTimeout);
        }

        var effectiveToken = timeoutCts?.Token ?? cancellationToken;
        string responseText;
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(httpRequest, effectiveToken);
            responseText = await response.Content.ReadAsStringAsync(effectiveToken);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested && requestTimeout != Timeout.InfiniteTimeSpan)
        {
            throw new TimeoutException($"LM Studio request timed out after {(int)requestTimeout.TotalSeconds} seconds.", ex);
        }

        using (response)
        {

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"LM Studio returned HTTP {(int)response.StatusCode}: {responseText}");
            }
        }

        var parsed = JsonSerializer.Deserialize<ChatCompletionResponse>(responseText, _jsonOptions);
        var content = parsed?.Choices?.FirstOrDefault()?.Message?.Content;

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("LM Studio response does not contain assistant text. Raw response:\n" + responseText);
        }

        return content.Trim();
    }

    public async Task TryUnloadAsync(LmStudioSettings settings, Action<string> log, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(settings.UnloadUrl))
        {
            log("Calling configured LM Studio unload URL...");
            using var request = new HttpRequestMessage(HttpMethod.Post, settings.UnloadUrl);
            if (!string.IsNullOrWhiteSpace(settings.ApiKey))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey.Trim());
            }

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                log("LM Studio unload URL completed.");
            }
            else
            {
                var warning = $"LM Studio unload URL returned HTTP {(int)response.StatusCode}.";
                log(warning);
                if (!settings.ContinueIfUnloadFails)
                {
                    throw new InvalidOperationException(warning);
                }
            }
            return;
        }

        if (!string.IsNullOrWhiteSpace(settings.UnloadCommand))
        {
            await RunUnloadCommandAsync(settings, log, cancellationToken);
            return;
        }

        log("No LM Studio unload URL/command configured; continuing without automatic unload.");
    }

    private static async Task RunUnloadCommandAsync(LmStudioSettings settings, Action<string> log, CancellationToken cancellationToken)
    {
        var timeout = TimeSpan.FromSeconds(Math.Max(1, settings.UnloadCommandTimeoutSeconds));
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);

        var startInfo = BuildUnloadStartInfo(settings.UnloadCommand);
        log("Running LM Studio unload command: " + settings.UnloadCommand);

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        var output = new StringBuilder();
        var error = new StringBuilder();
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null) output.AppendLine(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null) error.AppendLine(e.Data);
        };

        try
        {
            if (!process.Start())
            {
                HandleUnloadFailure("LM Studio unload command could not be started.", settings, log);
                return;
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync(timeoutCts.Token);

            if (output.Length > 0)
            {
                log("LM Studio unload stdout: " + output.ToString().Trim());
            }
            if (error.Length > 0)
            {
                log("LM Studio unload stderr: " + error.ToString().Trim());
            }

            log("LM Studio unload command exit code: " + process.ExitCode);
            if (process.ExitCode != 0)
            {
                HandleUnloadFailure("LM Studio unload command failed with exit code " + process.ExitCode + ".", settings, log);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            TryKill(process, log);
            HandleUnloadFailure($"LM Studio unload command timed out after {(int)timeout.TotalSeconds} seconds.", settings, log);
        }
        catch (Exception ex) when (settings.ContinueIfUnloadFails)
        {
            log("LM Studio unload command warning: " + ex.Message);
        }
    }

    private static ProcessStartInfo BuildUnloadStartInfo(string command)
    {
        var trimmed = command.Trim();
        var (executable, arguments) = SplitCommandLine(trimmed);
        var extension = Path.GetExtension(executable).ToLowerInvariant();

        if (extension == ".ps1")
        {
            return new ProcessStartInfo("powershell")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File {Quote(executable)} {arguments}".TrimEnd(),
                WorkingDirectory = Path.GetDirectoryName(executable) ?? Environment.CurrentDirectory
            };
        }

        if (extension is ".bat" or ".cmd")
        {
            return new ProcessStartInfo("cmd")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                Arguments = $"/c {Quote(executable)} {arguments}".TrimEnd(),
                WorkingDirectory = Path.GetDirectoryName(executable) ?? Environment.CurrentDirectory
            };
        }

        if (extension == ".exe" || File.Exists(executable))
        {
            return new ProcessStartInfo(executable)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                Arguments = arguments,
                WorkingDirectory = Path.GetDirectoryName(executable) ?? Environment.CurrentDirectory
            };
        }

        return new ProcessStartInfo("cmd")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            Arguments = "/c " + trimmed
        };
    }

    private static (string Executable, string Arguments) SplitCommandLine(string command)
    {
        if (command.StartsWith('"'))
        {
            var endQuote = command.IndexOf('"', 1);
            if (endQuote > 0)
            {
                return (command[1..endQuote], command[(endQuote + 1)..].TrimStart());
            }
        }

        var firstSpace = command.IndexOf(' ');
        return firstSpace < 0
            ? (command, "")
            : (command[..firstSpace], command[(firstSpace + 1)..].TrimStart());
    }

    private static string Quote(string value)
    {
        return value.Contains(' ') ? "\"" + value + "\"" : value;
    }

    private static void TryKill(Process process, Action<string> log)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                log("Timed out unload command process was stopped.");
            }
        }
        catch (Exception ex)
        {
            log("Could not stop timed out unload command process: " + ex.Message);
        }
    }

    private static void HandleUnloadFailure(string warning, LmStudioSettings settings, Action<string> log)
    {
        log(warning);
        if (!settings.ContinueIfUnloadFails)
        {
            throw new InvalidOperationException(warning);
        }
    }

    public static string BuildChatCompletionsUrl(string rawEndpoint)
    {
        var endpoint = rawEndpoint.Trim().TrimEnd('/');

        if (endpoint.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
        {
            return endpoint;
        }

        if (endpoint.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
        {
            return endpoint + "/chat/completions";
        }

        return endpoint + "/v1/chat/completions";
    }
}
