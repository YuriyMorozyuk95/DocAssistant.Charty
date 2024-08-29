

public interface IEmbedService
{
    /// <summary>
    /// Embeds the given pdf blob into the embedding service.
    /// </summary>
    /// <param name="blobStream">The stream from the blob to embed.</param>
    /// <param name="blobName">The name of the blob.</param>
    /// <returns>
    /// An asynchronous operation that yields <c>true</c>
    /// when successfully embedded, otherwise <c>false</c>.
    /// </returns>
    Task<bool> EmbedPdfBlob(
        Stream blobStream,
        string blobName);

    /// <summary>
    /// Embeds the given image blob into the embedding service.
    /// </summary>
    Task<bool> EmbedImageBlob(
        Stream imageStream,
        string imageUrl,
        string imageName,
        CancellationToken ct = default);

    Task CreateSearchIndex(string searchIndexName, CancellationToken ct = default);

    Task EnsureSearchIndex(string searchIndexName, CancellationToken ct = default);
}
