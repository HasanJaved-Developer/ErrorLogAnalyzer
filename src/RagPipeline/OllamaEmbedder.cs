using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace RagPipeline;

public class OllamaEmbedder(HttpClient httpClient, string ollamaEndpoint, ILogger<OllamaEmbedder> logger)
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    private const int MaxAttempts = 5;

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
    {
        var baseUrl = ollamaEndpoint.TrimEnd('/').Replace("/v1", "");
        var url     = $"{baseUrl}/api/embed";

        var body  = JsonSerializer.Serialize(new { model = "nomic-embed-text", input = text });
        var delay = TimeSpan.FromSeconds(2);

        for (var attempt = 1; ; attempt++)
        {
            using var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response      = await httpClient.PostAsync(url, content, ct);
            var responseBody  = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                if (attempt < MaxAttempts)
                {
                    logger.LogWarning(
                        "Ollama embed call to {Url} returned {Status} (attempt {Attempt}/{MaxAttempts}) — retrying in {Delay}s. Is 'nomic-embed-text' pulled yet? Response: {Response}",
                        url, response.StatusCode, attempt, MaxAttempts, delay.TotalSeconds, responseBody);
                    await Task.Delay(delay, ct);
                    delay += delay;
                    continue;
                }

                throw new HttpRequestException($"Ollama returned {response.StatusCode}: {responseBody}");
            }

            var result = JsonSerializer.Deserialize<EmbeddingResponse>(responseBody, _json)!;

            if (result.Embeddings is not { Length: > 0 } || result.Embeddings[0].Length == 0)
                throw new InvalidOperationException($"Ollama returned empty embeddings — is 'nomic-embed-text' pulled? Raw response: {responseBody}");

            return result.Embeddings[0];
        }
    }

    private record EmbeddingResponse(float[][] Embeddings);
}
