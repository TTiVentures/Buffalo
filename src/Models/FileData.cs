namespace Buffalo.Models
{
    public class FileData
    {
        public Stream? Data { get; set; }
        public string? FileName { get; set; }
        public string MimeType { get; set; } = "application/octet-stream";

    }
}
