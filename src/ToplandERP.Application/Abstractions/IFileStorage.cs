namespace ToplandERP.Application.Abstractions;

public interface IFileStorage
{
    Task<StoredFile> SaveAsync(
        Stream content,
        string originalFileName,
        string contentType,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(string storedPath, CancellationToken cancellationToken = default);
}

public sealed record StoredFile(
    string StoredPath,
    string OriginalFileName,
    string ContentType,
    long Length);
