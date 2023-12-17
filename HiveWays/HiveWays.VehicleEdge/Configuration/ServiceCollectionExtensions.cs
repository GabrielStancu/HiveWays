using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HiveWays.VehicleEdge.Configuration;

public static class ServiceCollectionExtensions 
{
    public static OptionsBuilder<T> AddConfiguration<T>(this IServiceCollection services, string sectionName, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped) where T : class
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (string.IsNullOrWhiteSpace(sectionName))
        {
            throw new ArgumentNullException(nameof(sectionName));
        }

        services.Add(new ServiceDescriptor(typeof(T), provider =>
        {
            var options = provider.GetRequiredService<IOptions<T>>();
            return options.Value;
        }, serviceLifetime));

        return services.AddOptions<T>().Configure<IConfiguration>((customSetting, configuration) =>
        {
            configuration.GetSection(sectionName).Bind(customSetting);
        });
    }
}
