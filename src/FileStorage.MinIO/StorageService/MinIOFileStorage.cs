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
    
    private readonly string _bucketName = options.Value.BucketName;

    public async Task<string> PutFileAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken)
    {
        await EnsureBucketExists(_bucketName, cancellationToken).ConfigureAwait(false);

        var compressedData = await compressor.CompressAsync(content, fileName, cancellationToken).ConfigureAwait(false);
        
        var id = idGenerator.CreateId();
        
        var putArgs = new PutObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(id)
            .WithStreamData(compressedData)
            .WithObjectSize(compressedData.Length)
            .WithContentType(contentType);
        
        await _minioClient.PutObjectAsync(putArgs, cancellationToken).ConfigureAwait(false);

        return id;
    }

    public async Task<(Stream content, string fileName, string contentType)> GetFileAsync(string id, CancellationToken cancellationToken)
    {
        await EnsureBucketExists(_bucketName, cancellationToken).ConfigureAwait(false);
        
        var statObjectArgs = new StatObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(id);
        var objectStat = await _minioClient.StatObjectAsync(statObjectArgs, cancellationToken).ConfigureAwait(false);

        var compressedContent = new MemoryStream((int)objectStat.Size);
        
        var getObjectArgs = new GetObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(id)
            .WithCallbackStream(stream => stream.CopyTo(compressedContent));
        
        await _minioClient.GetObjectAsync(getObjectArgs, cancellationToken).ConfigureAwait(false);        
        
        compressedContent.Seek(0, SeekOrigin.Begin);

        var (content, fileName) = await compressor.DecompressAsync(compressedContent, cancellationToken).ConfigureAwait(false);

        return (content, fileName, objectStat.ContentType);
    }

    private async Task EnsureBucketExists(string bucketName, CancellationToken cancellationToken)
    {
        var bucketExistArgs = new BucketExistsArgs()
            .WithBucket(bucketName);
        
        if (await _minioClient.BucketExistsAsync(bucketExistArgs, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var makeBucketArgs = new MakeBucketArgs()
            .WithBucket(bucketName);

        await _minioClient.MakeBucketAsync(makeBucketArgs, cancellationToken).ConfigureAwait(false);
    }
}