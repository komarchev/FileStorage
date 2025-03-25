using FileStorage.Common;

namespace FileStorage.WebApi.Services;

public class GuidIdGenerator : IIdGenerator
{
    public string CreateId()
    {
        return Guid.NewGuid().ToString("N");
    }
}