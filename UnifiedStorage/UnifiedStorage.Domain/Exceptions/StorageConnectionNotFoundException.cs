using UnifiedStorage.Domain.Enums;

namespace UnifiedStorage.Domain.Exceptions;

public class StorageConnectionNotFoundException : DomainException
{
    public StorageConnectionNotFoundException(string userId, StorageProvider provider)
        : base($"No active storage connection found for provider '{provider}' and user '{userId}'.") { }
}
