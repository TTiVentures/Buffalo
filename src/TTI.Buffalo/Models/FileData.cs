namespace TTI.Buffalo.Models;
public class FileData
{
    public FileData(Stream data, string fileName, string mimeType)
    {
        Data = data;
        FileName = fileName;
        MimeType = mimeType;
    }
    public Stream Data { get; set; }
    public string FileName { get; set; } = "file.bin";
    public string MimeType { get; set; } = "application/octet-stream";

}
