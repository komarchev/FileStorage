namespace FileStorage.Common;

public interface IFileStorage
{
    Task SaveFileAsync(string path, Stream content, string contentType, CancellationToken cancellationToken);
    
    Task<(string contentType, ReadOnlyMemory<byte> content)> LoadFileAsync(string path, CancellationToken cancellationToken);
}