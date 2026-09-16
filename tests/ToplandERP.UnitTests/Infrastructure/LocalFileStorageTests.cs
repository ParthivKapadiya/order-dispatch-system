using FluentAssertions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ToplandERP.Infrastructure.Options;
using ToplandERP.Infrastructure.Storage;

namespace ToplandERP.UnitTests.Infrastructure;

public class LocalFileStorageTests
{
    [Fact]
    public async Task Valid_photo_and_pdf_uploads_are_accepted()
    {
        var root = CreateTempRoot();
        var storage = CreateStorage(root, 10_000);
        await using var photo = new MemoryStream("jpeg-bytes"u8.ToArray());
        var savedPhoto = await storage.SaveAsync(photo, "material.jpg", "image/jpeg");
        savedPhoto.OriginalFileName.Should().Be("material.jpg");
        File.Exists(Path.Combine(root, savedPhoto.StoredPath)).Should().BeTrue();

        await using var pdf = new MemoryStream("%PDF"u8.ToArray());
        var savedPdf = await storage.SaveAsync(pdf, "lr.pdf", "application/pdf");
        savedPdf.OriginalFileName.Should().Be("lr.pdf");
    }

    [Fact]
    public async Task Executable_upload_is_rejected()
    {
        var storage = CreateStorage(CreateTempRoot(), 10_000);
        await using var content = new MemoryStream("binary"u8.ToArray());
        var act = async () => await storage.SaveAsync(content, "payload.exe", "application/octet-stream");
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Invalid file type.");
    }

    [Fact]
    public async Task Oversized_upload_is_rejected()
    {
        var storage = CreateStorage(CreateTempRoot(), 8);
        await using var content = new MemoryStream("0123456789"u8.ToArray());
        var act = async () => await storage.SaveAsync(content, "photo.jpg", "image/jpeg");
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("File size exceeds the allowed limit.");
    }

    private static LocalFileStorage CreateStorage(string root, long maxBytes)
    {
        return new LocalFileStorage(
            new TestHostEnvironment { ContentRootPath = root },
            Options.Create(new FileStorageOptions { RootPath = root, MaxFileSizeBytes = maxBytes }),
            NullLogger<LocalFileStorage>.Instance);
    }

    private static string CreateTempRoot()
    {
        var path = Path.Combine(Path.GetTempPath(), "topland-uploads", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
