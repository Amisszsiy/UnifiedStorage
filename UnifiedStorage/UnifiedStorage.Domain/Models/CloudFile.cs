using UnifiedStorage.Domain.Enums;

namespace UnifiedStorage.Domain.Models;

public class CloudFile
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public long SizeBytes { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public StorageProvider Provider { get; set; }
    public string? DownloadUrl { get; set; }
    public bool IsFolder { get; set; }
    public string? MimeType { get; set; }
    public string? ParentId { get; set; }
}
