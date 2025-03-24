namespace FileStorage.Common;

/// <summary>
/// Интерфейс генератора идентификаторов.
/// </summary>
public interface IIdGenerator
{
    /// <summary>
    /// Сгенерировать идентификатор.
    /// </summary>
    /// <returns>Новый идентификатор.</returns>
    string CreateId();
}