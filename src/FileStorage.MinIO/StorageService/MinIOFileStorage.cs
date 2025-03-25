using FileStorage.Common;
using FileStorage.MinIO.Configuration;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace FileStorage.MinIO.StorageService;

public class MinIOFileStorage(IIdGenerator idGenerator, ICompressor compressor, IOptions<MinIOConfiguration> options) : IFileStorage
{
    private readonly IMinioClient _minioClient = new MinioClient()
        .WithEndpoint(new Uri(options.Value.Uri))
        .WithCredentials(options.Value.AccessKey, options.Value.SecretKey)
        .Build(); 
    
    public async Task<string> PutFileAsync(Stream content, string fileName, string category, CancellationToken cancellationToken)
    {
        await EnsureBucketExists(category, cancellationToken).ConfigureAwait(false);

        var compressedData = await compressor.CompressAsync(content, fileName, cancellationToken).ConfigureAwait(false);
        
        var id = idGenerator.CreateId();

        var putArgs = new PutObjectArgs()
            .WithBucket(category)
            .WithObject(id)
            .WithStreamData(compressedData)
            .WithObjectSize(compressedData.Length);
        
        await _minioClient.PutObjectAsync(putArgs, cancellationToken).ConfigureAwait(false);

        return id;
    }

    public async Task<(Stream content, string fileName)> GetFileAsync(string id, string category, CancellationToken cancellationToken)
    {
        await EnsureBucketExists(category, cancellationToken).ConfigureAwait(false);
        
        var statObjectArgs = new StatObjectArgs()
            .WithBucket(category)
            .WithObject(id);
        var objectStat = await _minioClient.StatObjectAsync(statObjectArgs, cancellationToken).ConfigureAwait(false);

        var compressedContent = new MemoryStream((int)objectStat.Size);
        
        var getObjectArgs = new GetObjectArgs()
            .WithBucket(category)
            .WithObject(id)
            .WithCallbackStream(stream => stream.CopyTo(compressedContent));
        
        await _minioClient.GetObjectAsync(getObjectArgs, cancellationToken).ConfigureAwait(false);        
        
        compressedContent.Seek(0, SeekOrigin.Begin);

        var (content, fileName) = await compressor.DecompressAsync(compressedContent, cancellationToken).ConfigureAwait(false);

        return (content, fileName);
    }

    public Task<bool> CheckFileAsync(string id, string category, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteFileAsync(string id, string category, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    private async Task EnsureBucketExists(string category, CancellationToken cancellationToken)
    {
        var bucketExistArgs = new BucketExistsArgs()
            .WithBucket(category);
        
        if (await _minioClient.BucketExistsAsync(bucketExistArgs, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var makeBucketArgs = new MakeBucketArgs()
            .WithBucket(category);

        await _minioClient.MakeBucketAsync(makeBucketArgs, cancellationToken).ConfigureAwait(false);
    }
}