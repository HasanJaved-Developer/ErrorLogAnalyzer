using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace RagPipeline;

public class QdrantStore
{
    private readonly QdrantClient _client;
    private readonly string _collectionName;
    private const ulong VectorSize = 768; // nomic-embed-text output dimension

    public QdrantStore(IConfiguration config)
    {
        var host = config["Qdrant:Host"] ?? "localhost";
        _collectionName = config["Qdrant:CollectionName"] ?? "error-log-analyzer";
        _client = new QdrantClient(host);
    }

    /// <summary>Returns true if the collection was created, false if it already existed.</summary>
    public async Task<bool> EnsureCollectionAsync(CancellationToken ct = default)
    {
        var collections = await _client.ListCollectionsAsync(ct);
        if (collections.Any(c => c == _collectionName))
            return false;

        await _client.CreateCollectionAsync(_collectionName,
            new VectorParams { Size = VectorSize, Distance = Distance.Cosine },
            cancellationToken: ct);

        return true;
    }

    public async Task UpsertAsync(string filePath, int chunkIndex, float[] vector, string chunkText,
        CancellationToken ct = default)
    {
        var id = DeterministicId(filePath, chunkIndex);

        var qdrantVector = new Vector();
        qdrantVector.Data.AddRange(vector);

        await _client.UpsertAsync(_collectionName,
        [
            new PointStruct
            {
                Id      = new PointId { Uuid = id.ToString() },
                Vectors = new Vectors { Vector = qdrantVector },
                Payload =
                {
                    ["chunk_text"] = chunkText,
                    ["file_path"]  = filePath
                }
            }
        ], cancellationToken: ct);
    }

    public async Task<IReadOnlyList<string>> SearchAsync(float[] queryVector, int topK = 5,
        CancellationToken ct = default)
    {
        var results = await _client.SearchAsync(_collectionName, queryVector, limit: (ulong)topK,
            cancellationToken: ct);

        return results
            .Where(r => r.Payload.ContainsKey("chunk_text"))
            .Select(r => r.Payload["chunk_text"].StringValue)
            .ToList();
    }

    private static Guid DeterministicId(string filePath, int chunkIndex)
    {
        var key   = $"{filePath}::{chunkIndex}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        // Use first 16 bytes as a Guid
        return new Guid(bytes[..16]);
    }
}
