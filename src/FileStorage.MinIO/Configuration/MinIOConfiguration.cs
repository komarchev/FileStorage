namespace FileStorage.MinIO.Configuration;

public class MinIOConfiguration
{
    public string Uri { get; set; } = null!;
    
    public string AccessKey { get; set; } = null!;
    
    public string SecretKey { get; set; } = null!;
}