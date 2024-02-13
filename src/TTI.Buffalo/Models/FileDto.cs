namespace TTI.Buffalo.Models;

public class FileDto
{
    public Guid FileId { get; set; }
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
    public string? UploadedBy { get; set; }
    public AccessLevels AccessType { get; set; }
    public SecurityClaims? RequiredClaims { get; set; }
    public DateTime CreatedOn { get; set; }
    public string? ResourceUri { get; set; }
}