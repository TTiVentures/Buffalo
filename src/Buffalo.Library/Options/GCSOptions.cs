using System.ComponentModel.DataAnnotations;

namespace Buffalo.Options
{
    public class GCSOptions
    {
        [Required]
        public string? JsonCredentialsFile { get; set; }
        [Required]
        public string? StorageBucket { get; set; }
    }
}
