using UnifiedStorage.Domain.Enums;

namespace UnifiedStorage.Domain.Exceptions;

public class ReAuthRequiredException : DomainException
{
    public StorageProvider Provider { get; }

    public ReAuthRequiredException(StorageProvider provider)
        : base($"Re-authorization required for provider: {provider}. Please reconnect your {provider} account.")
    {
        Provider = provider;
    }
}
