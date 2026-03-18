using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Incursa.OpenAI.ChatKit.AspNetCore;

/// <summary>
/// Registers the Razor-hosting services for the ChatKit ASP.NET Core package.
/// </summary>
public static class ChatKitAspNetCoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers the package options used by the ChatKit tag helpers and packaged frontend.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <param name="configure">Optional delegate used to configure <see cref="ChatKitAspNetCoreOptions" />.</param>
    /// <returns>The same <see cref="IServiceCollection" /> instance.</returns>
    public static IServiceCollection AddIncursaOpenAIChatKitAspNetCore(
        this IServiceCollection services,
        Action<ChatKitAspNetCoreOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        OptionsBuilder<ChatKitAspNetCoreOptions> optionsBuilder = services.AddOptions<ChatKitAspNetCoreOptions>();
        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }

        return services;
    }

    /// <summary>
    /// Registers the package options used by the ChatKit tag helpers and packaged frontend.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <param name="configuration">The configuration section that binds <see cref="ChatKitAspNetCoreOptions" />.</param>
    /// <returns>The same <see cref="IServiceCollection" /> instance.</returns>
    public static IServiceCollection AddIncursaOpenAIChatKitAspNetCore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<ChatKitAspNetCoreOptions>()
            .Bind(configuration);

        return services;
    }
}
