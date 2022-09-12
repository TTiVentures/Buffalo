using Microsoft.AspNetCore.StaticFiles;

namespace Buffalo.Library.Utils
{
	public class MimeTypeTool
	{
		public static string GetMimeType(string fileName)
		{
			FileExtensionContentTypeProvider provider = new();
			if (!provider.TryGetContentType(fileName, out string? contentType))
			{
				contentType = "application/octet-stream";
			}
			return contentType;
		}
	}
}