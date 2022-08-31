using Buffalo.Implementations;
using Buffalo.Options;
using Microsoft.Extensions.DependencyInjection;


namespace Buffalo.Extensions.DependencyInjection
{


    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBuffalo(
            this IServiceCollection services, Action<IBuffaloBuilder> builderAction = null)
        {

            // Register lib services here...
            // services.AddScoped<ILibraryService, DefaultLibraryService>();;

            var options = new BuffaloBuilder(services);

            builderAction?.Invoke(options);

            return services;
        }

        public static IBuffaloBuilder UseAmazonS3(this IBuffaloBuilder me, Action<S3Options> options)
        {

            me.Services.AddOptions<S3Options>()
                .Configure(options)
                .ValidateDataAnnotations();

            me.Services.AddSingleton<IStorage, AmazonS3>();

            return me;
        }

        public static IBuffaloBuilder UseCloudStorage(this IBuffaloBuilder me, Action<GCSOptions> options)
        {
            me.Services.AddOptions<GCSOptions>()
                .Configure(options)
                .ValidateDataAnnotations();

            me.Services.AddSingleton<IStorage, GoogleCloudStorage>();

            return me;
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
