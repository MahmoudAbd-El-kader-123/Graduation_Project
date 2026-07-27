using Microsoft.Extensions.Configuration;
using SPIP.Application.Interfaces.Storage;

namespace SPIP.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;

    public LocalFileStorageService(IConfiguration configuration)
    {
        _basePath = configuration["FileStorage:BasePath"] ?? "App_Data/Invoices";
    }

    public async Task<string> SaveFileAsync(Stream fileStream, string fileName)
    {
        var (storedPath, _) = await SaveFileWithGuidAsync(fileStream, fileName, "application/octet-stream");
        return storedPath;
    }

    public async Task<(string StoredPath, string StoredFileName)> SaveFileWithGuidAsync(Stream fileStream, string originalFileName, string contentType, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_basePath);

        var extension = Path.GetExtension(originalFileName);
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var storedPath = Path.Combine(_basePath, storedFileName);

        await using var output = new FileStream(storedPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        if (fileStream.CanSeek)
            fileStream.Position = 0;

        await fileStream.CopyToAsync(output, cancellationToken);
        return (storedPath, storedFileName);
    }

    public Task<Stream> GetFileAsync(string path)
    {
        Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        return Task.FromResult(stream);
    }

    public Task DeleteFileAsync(string path)
    {
        if (File.Exists(path))
            File.Delete(path);

        return Task.CompletedTask;
    }
}
