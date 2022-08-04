using Buffalo.Models;
using System.Collections.Generic;

namespace Buffalo.Dto
{
	public class FileDto
	{
		public Guid FileId { get; set; }
		public string? FileName { get; set; }
		public string? ContentType { get; set; }
		public string? UploadedBy { get; set; }
		public AccessModes AccessType { get; set; }
		public DateTime CreatedOn { get; set; }
		public string? PublicUri { get; set; }
	}
}