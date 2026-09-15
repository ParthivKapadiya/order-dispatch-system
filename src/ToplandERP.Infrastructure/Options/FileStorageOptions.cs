namespace ToplandERP.Infrastructure.Options;

public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    public string RootPath { get; set; } = "App_Data/uploads";

    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;
}
