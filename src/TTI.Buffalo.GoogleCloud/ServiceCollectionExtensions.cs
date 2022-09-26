using Microsoft.Extensions.DependencyInjection;
using static TTI.Buffalo.ServiceCollectionExtensions;

namespace TTI.Buffalo.GoogleCloud
{
	public static class ServiceCollectionExtensions
	{
		public static IBuffaloBuilder UseCloudStorage(this IBuffaloBuilder me, Action<GCSOptions> options)
		{
			me.Services.AddOptions<GCSOptions>()
				.Configure(options)
				.ValidateDataAnnotations();

			me.Services.AddScoped<IStorage, GoogleCloudStorage>();

			return me;
		}
	}
}
