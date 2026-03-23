namespace UnifiedStorage.Application.Files.Queries.DownloadFile;

public class DownloadFileResult
{
    public Stream Content { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
}
