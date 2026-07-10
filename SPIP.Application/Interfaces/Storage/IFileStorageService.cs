namespace SPIP.Application.Interfaces.Storage;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(Stream fileStream, string fileName);
    Task<Stream> GetFileAsync(string path);
    Task DeleteFileAsync(string path);
}
