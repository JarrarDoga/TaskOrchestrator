namespace TaskOrchestrator.Api.Services.FileStorage;

public interface IFileStorageService
{
    /// <summary>
    /// Persists the stream and returns a relative storage path.
    /// The path is stored in the DB; the URL for download is constructed by the API.
    /// </summary>
    Task<string> SaveAsync(Stream content, string fileName, string contentType, CancellationToken ct = default);

    Task<(Stream Content, string ContentType)> ReadAsync(string storagePath, CancellationToken ct = default);

    Task DeleteAsync(string storagePath, CancellationToken ct = default);
}
