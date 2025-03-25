namespace FileStorage.MongoDb.GridFs.Configuration;

/// <summary>
/// Настройки подключения к MongoDB.
/// </summary>
public class MongoDbConfiguration
{
    /// <summary>
    /// Строка подключения.
    /// </summary>
    public string Uri { get; set; } = null!;
    
    /// <summary>
    /// Учетная запись.
    /// </summary>
    public string Username { get; set; } = null!;
    
    /// <summary>
    /// Пароль.
    /// </summary>
    public string Password { get; set; } = null!;
    
    /// <summary>
    /// Наименование базы данных.
    /// </summary>
    public string Database { get; set; } = null!;
}