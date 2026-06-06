using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using FlowDesk.Core.Enums;
using FlowDesk.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FlowDesk.Infrastructure.Services;

public class AIReportService : IAIReportService
{
    private readonly IProjectRepository _projects;
    private readonly IMilestoneRepository _milestones;
    private readonly IDeliverableRepository _deliverables;
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<AIReportService> _logger;
    private readonly string _provider;

    public AIReportService(
        IProjectRepository projects,
        IMilestoneRepository milestones,
        IDeliverableRepository deliverables,
        HttpClient http,
        IConfiguration config,
        ILogger<AIReportService> logger)
    {
        _projects = projects;
        _milestones = milestones;
        _deliverables = deliverables;
        _http = http;
        _config = config;
        _logger = logger;
        _provider = config["AI_PROVIDER"] ?? "ollama";
    }

    public async IAsyncEnumerable<string> StreamReportAsync(
        Guid projectId,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var project = await _projects.GetByIdAsync(projectId);
        if (project is null)
        {
            yield return "[Project not found.]";
            yield break;
        }

        var milestones = (await _milestones.GetAllByProjectAsync(projectId))
            .OrderBy(m => m.Order).ToList();
        var deliverables = (await _deliverables.GetAllByProjectAsync(projectId)).ToList();

        var completedMilestones = milestones.Count(m => m.Status == MilestoneStatus.Completed);
        var approvedDeliverables = deliverables.Count(d => d.Status == DeliverableStatus.Approved);

        var sb = new StringBuilder();
        sb.AppendLine("You are an AI assistant for FlowDesk, a project management tool for agencies.");
        sb.AppendLine("Generate a concise professional project status report.");
        sb.AppendLine();
        sb.AppendLine($"Project: {project.Name}");
        if (!string.IsNullOrEmpty(project.Description))
            sb.AppendLine($"Description: {project.Description}");
        sb.AppendLine($"Progress: {completedMilestones}/{milestones.Count} milestones complete, {approvedDeliverables}/{deliverables.Count} deliverables approved");
        sb.AppendLine();

        if (milestones.Count > 0)
        {
            sb.AppendLine("Milestones:");
            foreach (var m in milestones)
                sb.AppendLine($"- {m.Title}: {m.Status}");
            sb.AppendLine();
        }

        if (deliverables.Count > 0)
        {
            sb.AppendLine("Deliverables:");
            foreach (var d in deliverables)
            {
                var suffix = d.Status == DeliverableStatus.Revision && !string.IsNullOrEmpty(d.RevisionNotes)
                    ? $" (revision requested: {d.RevisionNotes})"
                    : string.Empty;
                sb.AppendLine($"- {d.Name}: {d.Status}{suffix}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("Write a 3-paragraph status report:");
        sb.AppendLine("Paragraph 1: What has been accomplished so far.");
        sb.AppendLine("Paragraph 2: What is currently in progress and any blockers or revision requests.");
        sb.AppendLine("Paragraph 3: What comes next and the expected timeline to completion.");
        sb.AppendLine("Be concise and professional.");

        var prompt = sb.ToString();

        await foreach (var token in _provider == "gemini"
            ? StreamGeminiAsync(prompt, ct)
            : StreamOllamaAsync(prompt, ct))
        {
            yield return token;
        }
    }

    private async IAsyncEnumerable<string> StreamGeminiAsync(
        string prompt,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var apiKey = _config["GEMINI_API_KEY"];
        var model = _config["GEMINI_MODEL"] ?? "gemini-1.5-flash";

        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("GEMINI_API_KEY not configured — report generation skipped");
            yield return "\n\n[Report generation failed: GEMINI_API_KEY not configured.]";
            yield break;
        }

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:streamGenerateContent?key={apiKey}&alt=sse";
        var body = new
        {
            contents = new[] { new { parts = new[] { new { text = prompt } } } }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body)
        };

        HttpResponseMessage? response = null;
        string? geminiError = null;
        try
        {
            response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini API call failed");
            geminiError = "\n\n[Report generation failed. Please try again.]";
        }

        if (geminiError is not null)
        {
            yield return geminiError;
            yield break;
        }

        using var stream = await response!.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line is null) break;
            if (!line.StartsWith("data: ")) continue;

            var json = line["data: ".Length..];
            if (json == "[DONE]") break;

            JsonDocument doc;
            try { doc = JsonDocument.Parse(json); }
            catch { continue; }

            using (doc)
            {
                if (!doc.RootElement.TryGetProperty("candidates", out var candidates)) continue;
                var text = candidates[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();
                if (!string.IsNullOrEmpty(text))
                    yield return text;
            }
        }
    }

    private async IAsyncEnumerable<string> StreamOllamaAsync(
        string prompt,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var baseUrl = (_config["OLLAMA_BASE_URL"] ?? "http://localhost:11434").TrimEnd('/');
        var model = _config["OLLAMA_MODEL"] ?? "llama3.2";

        var body = new { model, prompt, stream = true };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/api/generate")
        {
            Content = JsonContent.Create(body)
        };

        HttpResponseMessage? response = null;
        string? ollamaError = null;
        try
        {
            response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ollama API call failed — is Ollama running at {Url}?", baseUrl);
            ollamaError = "\n\n[Report generation failed. Is Ollama running locally?]";
        }

        if (ollamaError is not null)
        {
            yield return ollamaError;
            yield break;
        }

        using var stream = await response!.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrWhiteSpace(line)) continue;

            JsonDocument doc;
            try { doc = JsonDocument.Parse(line); }
            catch { continue; }

            using (doc)
            {
                var root = doc.RootElement;
                if (root.TryGetProperty("response", out var tokenProp))
                {
                    var token = tokenProp.GetString();
                    if (!string.IsNullOrEmpty(token))
                        yield return token;
                }

                if (root.TryGetProperty("done", out var doneProp) && doneProp.GetBoolean())
                    break;
            }
        }
    }
}
