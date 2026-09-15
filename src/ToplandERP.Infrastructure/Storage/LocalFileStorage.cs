using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ToplandERP.Application.Abstractions;
using ToplandERP.Infrastructure.Options;

namespace ToplandERP.Infrastructure.Storage;

public sealed class LocalFileStorage : IFileStorage
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
        ".pdf"
    };

    private readonly string _rootPath;
    private readonly long _maxFileSizeBytes;
    private readonly ILogger<LocalFileStorage> _logger;

    public LocalFileStorage(
        IHostEnvironment environment,
        IOptions<FileStorageOptions> options,
        ILogger<LocalFileStorage> logger)
    {
        var configuredRoot = options.Value.RootPath;
        _rootPath = Path.IsPathRooted(configuredRoot)
            ? configuredRoot
            : Path.GetFullPath(Path.Combine(environment.ContentRootPath, configuredRoot));
        _maxFileSizeBytes = options.Value.MaxFileSizeBytes;
        _logger = logger;
    }

    public async Task<StoredFile> SaveAsync(
        Stream content,
        string originalFileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var safeOriginalName = Path.GetFileName(originalFileName);
        if (string.IsNullOrWhiteSpace(safeOriginalName))
        {
            throw new InvalidOperationException("A file name is required.");
        }

        var extension = Path.GetExtension(safeOriginalName);
        if (!AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("This file type is not allowed.");
        }

        if (content.CanSeek && content.Length > _maxFileSizeBytes)
        {
            throw new InvalidOperationException("The file exceeds the maximum allowed size.");
        }

        Directory.CreateDirectory(_rootPath);

        var storedName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var fullPath = Path.Combine(_rootPath, storedName);

        await using (var fileStream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, useAsync: true))
        {
            await content.CopyToAsync(fileStream, cancellationToken);
            if (fileStream.Length > _maxFileSizeBytes)
            {
                fileStream.Close();
                File.Delete(fullPath);
                throw new InvalidOperationException("The file exceeds the maximum allowed size.");
            }
        }

        _logger.LogInformation("Stored uploaded file as {StoredName}", storedName);

        return new StoredFile(storedName, safeOriginalName, contentType, new FileInfo(fullPath).Length);
    }

    public Task DeleteAsync(string storedPath, CancellationToken cancellationToken = default)
    {
        var safeName = Path.GetFileName(storedPath);
        if (string.IsNullOrWhiteSpace(safeName))
        {
            return Task.CompletedTask;
        }

        var fullPath = Path.Combine(_rootPath, safeName);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }
}
