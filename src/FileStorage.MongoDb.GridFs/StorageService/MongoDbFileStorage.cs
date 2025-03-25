using FileStorage.Common;
using FileStorage.MongoDb.GridFs.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace FileStorage.MongoDb.GridFs.StorageService;

/// <summary>
/// Реализация файлового хранилища на MongoDB
/// </summary>
public class MongoDbFileStorage(ICompressor compressor, IOptions<MongoDbConfiguration> options) : IFileStorage
{
    private readonly IMongoDatabase _dataBase = new MongoClient(options.Value.Uri).GetDatabase(options.Value.Database);

    /// <inheritdoc />
    public async Task<string> PutFileAsync(Stream content, string fileName, string category, CancellationToken cancellationToken)
    {
        var bucket = new GridFSBucket(_dataBase, new GridFSBucketOptions
        {
            BucketName = category
        });

        var compressedData = await compressor.CompressAsync(content, fileName, cancellationToken).ConfigureAwait(false);

        await using var stream = await bucket.OpenUploadStreamAsync(fileName, cancellationToken: cancellationToken).ConfigureAwait(false);

        var id = stream.Id.ToString();

        await compressedData.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);

        await stream.CloseAsync(cancellationToken).ConfigureAwait(false);

        return id;
    }

    /// <inheritdoc />
    public async Task<(Stream content, string fileName)> GetFileAsync(string id, string category, CancellationToken cancellationToken)
    {
        var bucket = new GridFSBucket(_dataBase, new GridFSBucketOptions
        {
            BucketName = category
        });

        var objectId = ObjectId.Parse(id);

        await using var stream = await bucket.OpenDownloadStreamAsync(objectId, cancellationToken: cancellationToken).ConfigureAwait(false);

        var (content, fileName) = await compressor.DecompressAsync(stream, cancellationToken).ConfigureAwait(false);

        return (content, fileName);
    }

    /// <inheritdoc />
    public async Task<bool> CheckFileAsync(string id, string category, CancellationToken cancellationToken)
    {
        var bucket = new GridFSBucket(_dataBase, new GridFSBucketOptions
        {
            BucketName = category
        });

        var objectId = ObjectId.Parse(id);

        var filter = Builders<GridFSFileInfo>.Filter.Eq(f => f.Id, objectId);

        using var cursor = await bucket.FindAsync(filter, cancellationToken: cancellationToken).ConfigureAwait(false);
        
        var exists = await cursor.AnyAsync(cancellationToken).ConfigureAwait(false);
        
        return exists;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteFileAsync(string id, string category, CancellationToken cancellationToken)
    {
        var bucket = new GridFSBucket(_dataBase, new GridFSBucketOptions
        {
            BucketName = category
        });
        
        var objectId = ObjectId.Parse(id);
        
        var filter = Builders<GridFSFileInfo>.Filter.Eq(f => f.Id, objectId);
        
        using var cursor = await bucket.FindAsync(filter, cancellationToken: cancellationToken).ConfigureAwait(false);
        
        var exists = await cursor.AnyAsync(cancellationToken).ConfigureAwait(false);

        if (!exists)
        {
            return false;
        }
        
        await bucket.DeleteAsync(objectId, cancellationToken).ConfigureAwait(false);
        
        return true;
    }
}