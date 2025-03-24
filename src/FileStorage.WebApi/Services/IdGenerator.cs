using System.Security.Cryptography;
using FileStorage.Common;

namespace FileStorage.WebApi.Services;

/// <summary>
/// Генератор идентификаторов.
/// </summary>
public class IdGenerator : IIdGenerator
{
    private const int BYTE_SIZE = 8;
    private const int DEFAULT_ID_SIZE = 32;
    
    private static readonly char[] _dictionary = "123456789abcdefghikmnoprstuvwxyz".ToCharArray();
    private readonly RandomNumberGenerator _rng;
    private readonly int _idSize;
    
    /// <summary>
    /// Инициализирует новый экземпляр <see cref="IdGenerator"/>.
    /// </summary>
    public IdGenerator() : this(DEFAULT_ID_SIZE)
    {
    }

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="IdGenerator"/>.
    /// </summary>
    /// <param name="idSize">Размер идентификатора (символов).</param>
    public IdGenerator(int idSize)
    {
        _rng = RandomNumberGenerator.Create();
        _idSize = idSize;
    }

    public string CreateId()
    {
        Span<byte> randomData = stackalloc byte[_idSize];
        _rng.GetNonZeroBytes(randomData);
        
        Span<char> result = stackalloc char[_idSize];
        for (var i = 0; i < _idSize; i++)
        {
            result[i] = _dictionary[randomData[i] & 0x1f];
        }
        
        return new string(result);
    }
}