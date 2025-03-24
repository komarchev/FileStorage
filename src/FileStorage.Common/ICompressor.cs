namespace FileStorage.Common;

/// <summary>
/// Интерфейс архиватора.
/// </summary>
public interface ICompressor
{
    Task<Stream> CompressAsync(Stream input, string fileName, CancellationToken cancellationToken);
    Task<(Stream content, string fileName)> DecompressAsync(Stream input, CancellationToken cancellationToken);
}