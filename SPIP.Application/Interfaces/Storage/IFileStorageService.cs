namespace SPIP.Application.Interfaces.Storage;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(Stream fileStream, string fileName);
    Task<(string StoredPath, string StoredFileName)> SaveFileWithGuidAsync(Stream fileStream, string originalFileName, string contentType, CancellationToken cancellationToken = default);
    Task<Stream> GetFileAsync(string path);
    Task DeleteFileAsync(string path);
}
