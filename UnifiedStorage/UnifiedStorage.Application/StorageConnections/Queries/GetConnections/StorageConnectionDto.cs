using UnifiedStorage.Domain.Enums;

namespace UnifiedStorage.Application.StorageConnections.Queries.GetConnections;

public class StorageConnectionDto
{
    public Guid Id { get; set; }
    public StorageProvider Provider { get; set; }
    public ConnectionStatus Status { get; set; }
    public DateTime LastUsedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsExpiringSoon { get; set; }
}
