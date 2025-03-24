namespace FileStorage.Common;

public interface IFileStorage
{
    Task<string> PutFileAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken);
    
    Task<(string contentType, ReadOnlyMemory<byte> content)> GetFileAsync(string id, CancellationToken cancellationToken);
}