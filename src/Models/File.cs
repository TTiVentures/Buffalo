using System.ComponentModel.DataAnnotations;

namespace Buffalo.Models
{

    public enum AccessModes
    {
        PUBLIC = 0,
        PRIVATE = 1,
        PROTECTED = 2
    }

    public class File
    {

        [Key]
        public Guid FileId { get; set; }
        public string? FileName { get; set; }
        public string? ContentType { get; set; }
        public string? UploadedBy { get; set; }
        public AccessModes AccessType { get; set; }
        public DateTime CreatedOn { get; set; }
        public byte[]? FileHash { get; set; }
    }

}
