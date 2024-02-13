namespace TTI.Buffalo.Models;

public class FileData : FileInfo
{
    public required Stream Data { get; set; }
    public required string MimeType { get; set; }
}

public class FileInfo
{
    public required Guid Id { get; set; }
    public required IDictionary<string, string> Metadata { get; set; }

    public FileInfoDto ToDto()
    {
        return new()
        {
            Id = Id,
            Metadata = Metadata
        };
    }
}

public class FileInfoDto
{
    public required Guid Id { get; set; }
    public IDictionary<string, string>? Metadata { get; set; }
}