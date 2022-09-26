using System.ComponentModel.DataAnnotations;

namespace TTI.Buffalo.AmazonS3
{
	public class S3Options
	{
		[Required]
		public string? AccessKey { get; set; }
		[Required]
		public string? SecretKey { get; set; }
		[Required]
		public string? BucketName { get; set; }
		[Required]
		public string? RegionEndpoint { get; set; }
		public string? FolderName { get; set; }
	}
}
