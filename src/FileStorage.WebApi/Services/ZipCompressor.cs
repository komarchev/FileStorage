using System.IO.Compression;
using FileStorage.Common;

namespace FileStorage.WebApi.Services;

public class ZipCompressor : ICompressor
{
    public async Task<Stream> CompressAsync(Stream input, string fileName, CancellationToken cancellationToken)
    {
        var outputStream = new MemoryStream();

        var archive = new ZipArchive(outputStream, ZipArchiveMode.Create, leaveOpen: true);
        try
        {
            var entry = archive.CreateEntry(fileName, CompressionLevel.SmallestSize);
            await input.CopyToAsync(entry.Open(), cancellationToken);
        }
        finally
        {
            archive.Dispose();
        }

        outputStream.Seek(0, SeekOrigin.Begin);
        return outputStream;
    }

    public async Task<(Stream content, string fileName)> DecompressAsync(Stream input, CancellationToken cancellationToken)
    {
        var outputStream = new MemoryStream();
        string fileName;
        
        var archive = new ZipArchive(input, ZipArchiveMode.Read,  leaveOpen: true);

        try
        {
            var entry = archive.Entries[0];
            fileName = entry.FullName;
            var content = entry.Open();
            await content.CopyToAsync(outputStream, cancellationToken);
        }
        finally
        {
            archive.Dispose();
        }
        
        outputStream.Seek(0, SeekOrigin.Begin);
        return (outputStream, fileName);
    }
}