namespace TaskOrchestrator.Api.Services.FileStorage;

// Files live under the configured root (default: ./uploads/).
// Swap this implementation for S3/Azure Blob without touching callers.
public sealed class LocalFileStorageService(IConfiguration config) : IFileStorageService
{
    private readonly string _root = Path.GetFullPath(
        config["FileStorage:LocalPath"] ?? "uploads");

    public async Task<string> SaveAsync(Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        // Subdirectory by date keeps the folder manageable
        var sub = DateTime.UtcNow.ToString("yyyy/MM/dd");
        var dir = Path.Combine(_root, sub);
        Directory.CreateDirectory(dir);

        var safeFileName = $"{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
        var fullPath = Path.Combine(dir, safeFileName);

        await using var fs = File.Create(fullPath);
        await content.CopyToAsync(fs, ct);

        return Path.Combine(sub, safeFileName).Replace('\\', '/');
    }

    public Task<(Stream Content, string ContentType)> ReadAsync(string storagePath, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_root, storagePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath)) throw new FileNotFoundException("Attachment not found.", fullPath);

        // Content-type is stored in the DB; we just return the raw stream here
        Stream stream = File.OpenRead(fullPath);
        return Task.FromResult((stream, "application/octet-stream"));
    }

    public Task DeleteAsync(string storagePath, CancellationToken ct = default)
    {
        var fullPath = Path.Combine(_root, storagePath.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(fullPath)) File.Delete(fullPath);
        return Task.CompletedTask;
    }
}
