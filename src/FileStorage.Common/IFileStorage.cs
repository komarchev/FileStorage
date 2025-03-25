namespace FileStorage.Common;

public interface IFileStorage
{
    Task<string> PutFileAsync(Stream content, string fileName, string category, CancellationToken cancellationToken);
    
    Task<(Stream content, string fileName)> GetFileAsync(string id, string category, CancellationToken cancellationToken);
    
    Task<bool> CheckFileAsync(string id, string category, CancellationToken cancellationToken);
    
    Task<bool> DeleteFileAsync(string id, string category, CancellationToken cancellationToken);
}