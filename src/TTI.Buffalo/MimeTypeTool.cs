using Microsoft.AspNetCore.StaticFiles;

namespace TTI.Buffalo
{
	public class MimeTypeTool
	{
		public static string? GetMimeType(string fileName)
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