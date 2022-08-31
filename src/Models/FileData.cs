namespace Buffalo.Models
{
    public class FileData
    {
        public Stream? Data { get; set; }
        public string FileName { get; set; } = "file.bin";
        public string MimeType { get; set; } = "application/octet-stream";

    }
}
