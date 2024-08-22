using Shared.Models;

public interface ISearchService
{
    Task<SupportingContentRecord[]> QueryDocuments(
               string? query = null,
               float[]? embedding = null,
               RequestOverrides? overrides = null,
               CancellationToken cancellationToken = default);

    Task<SupportingImageRecord[]> QueryImages(
               string? query = null,
               float[]? embedding = null,
               RequestOverrides? overrides = null,
               CancellationToken cancellationToken = default);
}
