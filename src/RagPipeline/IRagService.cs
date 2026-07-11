namespace RagPipeline;

public interface IRagService
{
    Task IndexFilesAsync(IEnumerable<string> absoluteFilePaths, CancellationToken ct = default);
    Task<IReadOnlyList<string>> RetrieveAsync(string query, int topK = 5, CancellationToken ct = default);
}
