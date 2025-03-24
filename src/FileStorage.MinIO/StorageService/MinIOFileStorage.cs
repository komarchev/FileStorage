using System.Buffers;
using System.Net.Mime;
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
        await EnsureBucketExists(_bucketName, cancellationToken);

        var compressedData = await compressor.CompressAsync(content, fileName, cancellationToken);
        
        var id = idGenerator.CreateId();
        
        var putArgs = new PutObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(id)
            .WithStreamData(compressedData)
            .WithObjectSize(compressedData.Length)
            .WithContentType(MediaTypeNames.Application.Zip);
        
        await _minioClient.PutObjectAsync(putArgs, cancellationToken).ConfigureAwait(false);

        return id;
    }

    public async Task<(string contentType, ReadOnlyMemory<byte> content)> GetFileAsync(string id, CancellationToken cancellationToken)
    {
        var (contentType, content) = await ReadObject(id, cancellationToken).ConfigureAwait(false);
        
        return (contentType, content);
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

    private async Task<(string contentType, ReadOnlyMemory<byte> content)> ReadObject(string path, CancellationToken cancellationToken)
    {
        var statObjectArgs = new StatObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(path);
        
        var objectStat = await _minioClient.StatObjectAsync(statObjectArgs, cancellationToken).ConfigureAwait(false);
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

            await _minioClient.GetObjectAsync(getObjectArgs, cancellationToken).ConfigureAwait(false);

            var content = new ReadOnlyMemory<byte>(buffer, 0, objectSize);
            return (objectStat.ContentType, content);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}