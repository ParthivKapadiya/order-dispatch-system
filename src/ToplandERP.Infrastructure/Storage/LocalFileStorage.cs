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
        if (!AllowedExtensions.Contains(extension) || !IsAllowedContentType(extension, contentType))
        {
            throw new InvalidOperationException("Invalid file type.");
        }

        if (content.CanSeek && content.Length > _maxFileSizeBytes)
        {
            throw new InvalidOperationException("File size exceeds the allowed limit.");
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
                throw new InvalidOperationException("File size exceeds the allowed limit.");
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

    public Task<Stream> OpenReadAsync(string storedPath, CancellationToken cancellationToken = default)
    {
        var safeName = Path.GetFileName(storedPath);
        if (string.IsNullOrWhiteSpace(safeName))
        {
            throw new FileNotFoundException("The requested file was not found.");
        }

        var fullPath = Path.Combine(_rootPath, safeName);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("The requested file was not found.");
        }

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        return Task.FromResult(stream);
    }

    private static bool IsAllowedContentType(string extension, string contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return true;
        }

        var normalized = contentType.Split(';', 2)[0].Trim().ToLowerInvariant();
        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => normalized is "image/jpeg" or "image/jpg" or "image/pjpeg",
            ".png" => normalized == "image/png",
            ".webp" => normalized == "image/webp",
            ".pdf" => normalized == "application/pdf",
            _ => false
        };
    }
}
