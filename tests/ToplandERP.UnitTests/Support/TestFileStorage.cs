using ToplandERP.Application.Abstractions;

namespace ToplandERP.UnitTests.Support;

public sealed class TestFileStorage : IFileStorage
{
    public List<StoredFile> Saved { get; } = [];

    public Exception? SaveException { get; set; }

    public Task<StoredFile> SaveAsync(
        Stream content,
        string originalFileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (SaveException is not null)
        {
            throw SaveException;
        }

        var stored = new StoredFile($"{Guid.NewGuid():N}{Path.GetExtension(originalFileName)}", originalFileName, contentType, content.Length);
        Saved.Add(stored);
        return Task.FromResult(stored);
    }

    public Task DeleteAsync(string storedPath, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<Stream> OpenReadAsync(string storedPath, CancellationToken cancellationToken = default) =>
        Task.FromResult<Stream>(new MemoryStream("test"u8.ToArray()));
}
