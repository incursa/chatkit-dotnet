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
    public static IServiceCollection AddOpenAIChatKit(
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
    public static IServiceCollection AddOpenAIChatKit(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<ChatKitAspNetCoreOptions>()
            .Bind(configuration);

        return services;
    }

    /// <summary>
    /// Registers ChatKit UI options for OpenAI-hosted session and action endpoints.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <param name="configure">Optional delegate used to configure <see cref="ChatKitAspNetCoreOptions" />.</param>
    /// <returns>The same <see cref="IServiceCollection" /> instance.</returns>
    public static IServiceCollection AddOpenAIChatKitHosted(
        this IServiceCollection services,
        Action<ChatKitAspNetCoreOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddOpenAIChatKit(options =>
        {
            options.ApiUrl = null;
            options.DomainKey = null;
            configure?.Invoke(options);
        });
    }

    /// <summary>
    /// Registers ChatKit UI options for direct ChatKit API mode.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <param name="apiUrl">The ChatKit API URL.</param>
    /// <param name="domainKey">The optional domain key used with the API URL.</param>
    /// <param name="configure">Optional delegate used to configure <see cref="ChatKitAspNetCoreOptions" />.</param>
    /// <returns>The same <see cref="IServiceCollection" /> instance.</returns>
    public static IServiceCollection AddOpenAIChatKitApi(
        this IServiceCollection services,
        string apiUrl,
        string? domainKey = null,
        Action<ChatKitAspNetCoreOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiUrl);

        return services.AddOpenAIChatKit(options =>
        {
            options.ApiUrl = apiUrl;
            options.DomainKey = domainKey;
            configure?.Invoke(options);
        });
    }
}
