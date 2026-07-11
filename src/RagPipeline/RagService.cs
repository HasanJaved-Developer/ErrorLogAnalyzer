using Microsoft.Extensions.Logging;

namespace RagPipeline;

public class RagService(OllamaEmbedder embedder, QdrantStore store, ILogger<RagService> logger) : IRagService
{
    public async Task IndexFilesAsync(IEnumerable<string> absoluteFilePaths, CancellationToken ct = default)
    {
        var collectionCreated = await store.EnsureCollectionAsync(ct);
        if (!collectionCreated)
        {
            logger.LogInformation("Collection already exists — skipping indexing.");
            return;
        }

        foreach (var filePath in absoluteFilePaths)
        {
            if (!File.Exists(filePath))
            {
                logger.LogWarning("Seed file not found, skipping: {Path}", filePath);
                continue;
            }

            var text   = await File.ReadAllTextAsync(filePath, ct);
            var chunks = CodeChunker.Chunk(text).ToList();

            logger.LogInformation("Indexing {File} — {Count} chunks", Path.GetFileName(filePath), chunks.Count);

            for (int i = 0; i < chunks.Count; i++)
            {
                var vector = await embedder.EmbedAsync(chunks[i], ct);
                await store.UpsertAsync(filePath, i, vector, chunks[i], ct);
            }
        }
    }

    public async Task<IReadOnlyList<string>> RetrieveAsync(string query, int topK = 5, CancellationToken ct = default)
    {
        var queryVector = await embedder.EmbedAsync(query, ct);
        return await store.SearchAsync(queryVector, topK, ct);
    }
}
