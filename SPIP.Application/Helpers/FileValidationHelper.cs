namespace SPIP.Application.Helpers;

public static class FileValidationHelper
{
    public static bool ValidateFileSignature(Stream fileStream, string extension)
    {
        if (!fileStream.CanRead)
            return false;

        var originalPosition = fileStream.CanSeek ? fileStream.Position : 0;
        try
        {
            Span<byte> buffer = stackalloc byte[4];
            var bytesRead = fileStream.Read(buffer);
            extension = extension.ToLowerInvariant();

            return extension switch
            {
                ".pdf" => bytesRead >= 4 && buffer[0] == 0x25 && buffer[1] == 0x50 && buffer[2] == 0x44 && buffer[3] == 0x46,
                ".jpg" or ".jpeg" => bytesRead >= 2 && buffer[0] == 0xFF && buffer[1] == 0xD8,
                ".png" => bytesRead >= 4 && buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47,
                _ => false
            };
        }
        finally
        {
            if (fileStream.CanSeek)
                fileStream.Position = originalPosition;
        }
    }

    public static string GetContentTypeFromExtension(string extension) =>
        extension.ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => "application/octet-stream"
        };
}
