using UnifiedStorage.Domain.Enums;

namespace UnifiedStorage.Application.Files.Queries.GetFiles;

public class CloudFileDto
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public long SizeBytes { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public StorageProvider Provider { get; set; }
    public bool IsFolder { get; set; }
    public string? MimeType { get; set; }
    public string? ParentId { get; set; }
}
