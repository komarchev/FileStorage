using FileStorage.Common;
using FileStorage.FileSystem.Configuration;
using Microsoft.Extensions.Options;

namespace FileStorage.FileSystem.StorageService;

public class FileSystemStorage(IIdGenerator idGenerator, ICompressor compressor, IOptions<FileSystemConfiguration> options) : IFileStorage
{
    private readonly string _root = options.Value.RootPath;
    
    public async Task<string> PutFileAsync(Stream content, string fileName, string category, CancellationToken cancellationToken)
    {
        var directoryPath = Path.Combine(_root, category);
        
        Directory.CreateDirectory(directoryPath);
        
        var compressedData = await compressor.CompressAsync(content, fileName, cancellationToken).ConfigureAwait(false);
        
        var id = idGenerator.CreateId();

        var path = Path.Combine(directoryPath, id);

        var dataHash = await compressedData.GetHashAsync(cancellationToken).ConfigureAwait(false);

        await SaveFileAsync(path, compressedData, cancellationToken).ConfigureAwait(false);

        var storedHash = await GetFileHashAsync(path, cancellationToken).ConfigureAwait(false);

        if (!dataHash.SequenceEqual(storedHash))
        {
            throw new InvalidOperationException("The input file hash does not match the stored file hash.");
        }
        
        return id;
    }

    public async Task<(Stream content, string fileName)> GetFileAsync(string id, string category, CancellationToken cancellationToken)
    {
        var  path = Path.Combine(_root, category, id);
        
        await using var compressedContent = File.Open(path, FileMode.Open);
        
        var (content, fileName) = await compressor.DecompressAsync(compressedContent, cancellationToken).ConfigureAwait(false);

        return (content, fileName);
    }

    public Task<bool> CheckFileAsync(string id, string category, CancellationToken cancellationToken)
    {
        var path = Path.Combine(_root, category, id);
        
        var  exists = File.Exists(path);
        
        return Task.FromResult(exists);
    }

    public Task<bool> DeleteFileAsync(string id, string category, CancellationToken cancellationToken)
    {
        var path = Path.Combine(_root, category, id);
        
        var  exists = File.Exists(path);

        if (!exists)
        {
            return Task.FromResult(false);
        } 
        
        File.Delete(path);

        return Task.FromResult(true);
    }
    
    private static async Task SaveFileAsync(string path, Stream content, CancellationToken cancellationToken)
    {
        content.Seek(0, SeekOrigin.Begin);
        await using var fileStream = File.Create(path);
        await content.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<byte[]> GetFileHashAsync(string path, CancellationToken cancellationToken)
    {
        await using var fileStream = File.OpenRead(path);
        var fileHash = await fileStream.GetHashAsync(cancellationToken).ConfigureAwait(false);
        return fileHash;
    }
}