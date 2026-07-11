namespace RagPipeline;

public static class CodeChunker
{
    public static IEnumerable<string> Chunk(string text, int maxChars = 500, int overlapChars = 50)
    {
        if (string.IsNullOrEmpty(text))
            yield break;

        int start = 0;
        while (start < text.Length)
        {
            int length = Math.Min(maxChars, text.Length - start);
            yield return text.Substring(start, length);
            if (start + length >= text.Length)
                break;
            start += maxChars - overlapChars;
        }
    }
}
