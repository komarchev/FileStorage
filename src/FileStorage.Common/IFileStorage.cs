namespace FileStorage.Common;

public interface IFileStorage
{
    Task<string> PutFileAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken);
    
    Task<(Stream content, string fileName, string contentType)> GetFileAsync(string id, CancellationToken cancellationToken);
}