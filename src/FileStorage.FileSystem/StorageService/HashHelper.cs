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
}