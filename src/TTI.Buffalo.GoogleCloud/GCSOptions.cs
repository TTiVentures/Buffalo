using System.ComponentModel.DataAnnotations;

namespace TTI.Buffalo.GoogleCloud
{
	public class GCSOptions
	{
		[Required]
		public string? JsonCredentialsFile { get; set; }
		[Required]
		public string? StorageBucket { get; set; }
	}
}
