using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Incursa.OpenAI.ChatKit.AspNetCore.Tests;

[Trait("Category", "Unit")]
public sealed class ChatKitAspNetCoreServiceCollectionExtensionsTests
{
    /// <summary>The hosted service registration clears direct API defaults while preserving hosted configuration.</summary>
    /// <intent>Protect the DI setup that switches consumers from direct API mode to hosted session mode.</intent>
    /// <scenario>LIB-CHATKIT-ASPNETCORE-001</scenario>
    /// <behavior>Calling <c>AddOpenAIChatKitHosted</c> resets the API URL and domain key while keeping the hosted session endpoint.</behavior>
    [Fact]
    public void AddOpenAIChatKitHosted_ClearsApiModeDefaults()
    {
        ServiceProvider services = new ServiceCollection()
            .AddOpenAIChatKit(options =>
            {
                options.ApiUrl = "https://example.contoso.com/chatkit";
                options.DomainKey = "contoso-domain-key";
            })
            .AddOpenAIChatKitHosted(options =>
            {
                options.SessionEndpoint = "/api/chatkit/session";
            })
            .BuildServiceProvider();

        ChatKitAspNetCoreOptions options = services.GetRequiredService<IOptions<ChatKitAspNetCoreOptions>>().Value;

        Assert.Null(options.ApiUrl);
        Assert.Null(options.DomainKey);
        Assert.Equal("/api/chatkit/session", options.SessionEndpoint);
    }

    /// <summary>The API service registration seeds direct browser API defaults.</summary>
    /// <intent>Protect the DI setup that configures the ASP.NET Core wrapper for direct API mode.</intent>
    /// <scenario>LIB-CHATKIT-ASPNETCORE-001</scenario>
    /// <behavior>Calling <c>AddOpenAIChatKitApi</c> stores the configured API URL, domain key, and locale options.</behavior>
    [Fact]
    public void AddOpenAIChatKitApi_SetsApiModeDefaults()
    {
        ServiceProvider services = new ServiceCollection()
            .AddOpenAIChatKitApi(
                "https://example.contoso.com/chatkit",
                "contoso-domain-key",
                options =>
                {
                    options.Locale = "en";
                })
            .BuildServiceProvider();

        ChatKitAspNetCoreOptions options = services.GetRequiredService<IOptions<ChatKitAspNetCoreOptions>>().Value;

        Assert.Equal("https://example.contoso.com/chatkit", options.ApiUrl);
        Assert.Equal("contoso-domain-key", options.DomainKey);
        Assert.Equal("en", options.Locale);
    }
}
