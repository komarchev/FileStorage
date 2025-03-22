using System.Buffers;
using FileStorage.Common;
using FileStorage.MinIO.Configuration;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace FileStorage.MinIO.StorageService;

public class MinIOFileStorage(IOptions<MinIOConfiguration> options) : IFileStorage
{
    private readonly IMinioClient _minioClient = new MinioClient()
        .WithEndpoint(new Uri(options.Value.Uri))
        .WithCredentials(options.Value.AccessKey, options.Value.SecretKey)
        .Build(); 
    
    private readonly string _bucketName = options.Value.BucketName;

    public async Task SaveFileAsync(string path, Stream content, string contentType, CancellationToken cancellationToken)
    {
        await EnsureBucketExists(_bucketName, cancellationToken);
        
        var putArgs = new PutObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(path)
            .WithStreamData(content)
            .WithObjectSize(content.Length)
            .WithContentType(contentType);
        
        await _minioClient.PutObjectAsync(putArgs, cancellationToken);
    }

    public async Task<(string contentType, ReadOnlyMemory<byte> content)> LoadFileAsync(string path, CancellationToken cancellationToken)
    {
        var (contentType, content) = await ReadObject(path, cancellationToken);
        
        return (contentType, content);
    }

    private async Task EnsureBucketExists(string bucketName, CancellationToken cancellationToken)
    {
        var bucketExistArgs = new BucketExistsArgs()
            .WithBucket(bucketName);
        
        if (await _minioClient.BucketExistsAsync(bucketExistArgs, cancellationToken))
        {
            return;
        }

        var makeBucketArgs = new MakeBucketArgs()
            .WithBucket(bucketName);

        await _minioClient.MakeBucketAsync(makeBucketArgs, cancellationToken);
    }

    private async Task<(string contentType, ReadOnlyMemory<byte> content)> ReadObject(string path, CancellationToken cancellationToken)
    {
        var statObjectArgs = new StatObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(path);
        
        var objectStat = await _minioClient.StatObjectAsync(statObjectArgs, cancellationToken);
        var objectSize = (int)objectStat.Size;

        var buffer = ArrayPool<byte>.Shared.Rent(objectSize);

        try
        {
            var offset = 0;

            var getObjectArgs = new GetObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(path)
                .WithCallbackStream(stream =>
                {
                    while (offset < objectSize)
                    {
                        var read = stream.Read(buffer, offset, objectSize - offset);
                        offset += read;
                    }
                });

            await _minioClient.GetObjectAsync(getObjectArgs, cancellationToken);

            var content = new ReadOnlyMemory<byte>(buffer, 0, objectSize);
            return (objectStat.ContentType, content);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}