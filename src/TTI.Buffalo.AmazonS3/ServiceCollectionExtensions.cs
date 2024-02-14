using Microsoft.Extensions.DependencyInjection;
using static TTI.Buffalo.ServiceCollectionExtensions;

namespace TTI.Buffalo.AmazonS3;

public static class ServiceCollectionExtensions
{
    public static IBuffaloBuilder UseAmazonS3(this IBuffaloBuilder me, Action<S3Options> options)
    {
        me.Services.AddOptions<S3Options>()
            .Configure(options)
            .ValidateDataAnnotations();

        me.Services.AddScoped<IStorage, AmazonS3>();

        return me;
    }
}