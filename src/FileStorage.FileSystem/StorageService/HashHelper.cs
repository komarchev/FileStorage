using System.Security.Cryptography;

namespace FileStorage.FileSystem.StorageService;

public static class HashHelper
{
    public static async Task<byte[]> GetHashAsync(this Stream stream, CancellationToken cancellationToken)
    {
        var hash = SHA256.Create();
        var hashed = await hash.ComputeHashAsync(stream, cancellationToken).ConfigureAwait(false);
        stream.Seek(0, SeekOrigin.Begin);
        return hashed;
    }

    public static async Task<byte[]> GetHashAsync(this FileInfo fileInfo, CancellationToken cancellationToken)
    {
        var stream = fileInfo.OpenRead();
        return await GetHashAsync(stream, cancellationToken).ConfigureAwait(false);
    }
}