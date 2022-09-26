using Microsoft.Extensions.DependencyInjection;
using TTI.Buffalo;

namespace TTI.Buffalo
{
	public static class ServiceCollectionExtensions
	{
		public static IServiceCollection AddBuffalo(
			this IServiceCollection services, Action<IBuffaloBuilder> builderAction)
		{
			services.AddScoped<FileManager>();

			BuffaloBuilder? options = new(services);

			builderAction?.Invoke(options);

			return services;
		}
		public interface IBuffaloBuilder
		{
			IServiceCollection Services { get; }
		}
		internal class BuffaloBuilder : IBuffaloBuilder
		{
			public BuffaloBuilder(IServiceCollection services)
			{
				Services = services;
			}
			public IServiceCollection Services { get; }
		}
	}
}
