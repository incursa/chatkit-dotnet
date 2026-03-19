using System.Text.Json.Nodes;
using Incursa.OpenAI.ChatKit.AspNetCore.TagHelpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Incursa.OpenAI.ChatKit.AspNetCore.Tests;

[Trait("Category", "Unit")]
public sealed class IncursaChatKitTagHelperTests
{
    /// <summary>The host tag helper falls back to conventional ChatKit endpoints when no endpoints are configured.</summary>
    /// <intent>Protect the public Razor wrapper surface added to the ASP.NET Core package.</intent>
    /// <scenario>LIB-CHATKIT-ASPNETCORE-003</scenario>
    /// <behavior>Omitting session and action endpoints still serializes the conventional local routes into the host config.</behavior>
    [Fact]
    public async Task ProcessAsync_UsesDefaultBackendRoutes_WhenEndpointsAreOmitted()
    {
        ServiceProvider services = new ServiceCollection()
            .AddLogging()
            .AddIncursaOpenAIChatKitAspNetCore()
            .BuildServiceProvider();

        IncursaChatKitTagHelper tagHelper = CreateTagHelper(services);
        TagHelperOutput output = CreateOutput();

        await tagHelper.ProcessAsync(CreateContext(), output);

        Assert.Equal("div", output.TagName);
        Assert.Equal("true", output.Attributes["data-incursa-chatkit-host"]?.Value);

        JsonNode config = ParseConfig(output);
        Assert.Equal("/api/chatkit/session", config["sessionEndpoint"]?.GetValue<string>());
        Assert.Equal("/api/chatkit/action", config["actionEndpoint"]?.GetValue<string>());
        Assert.Equal("720px", config["height"]?.GetValue<string>());
    }

    /// <summary>The host tag helper serializes configured UI options and explicit attributes into the browser config payload.</summary>
    /// <intent>Protect the public Razor wrapper surface added to the ASP.NET Core package.</intent>
    /// <scenario>LIB-CHATKIT-ASPNETCORE-003</scenario>
    /// <behavior>Configured options and explicit tag helper attributes appear in the serialized host config.</behavior>
    [Fact]
    public async Task ProcessAsync_SerializesConfiguredUiOptions()
    {
        ServiceProvider services = new ServiceCollection()
            .AddLogging()
            .AddIncursaOpenAIChatKitAspNetCore(options =>
            {
                options.DefaultHeight = "840px";
                options.Locale = "en";
                options.FrameTitle = "Workspace assistant";
                options.Theme.ColorScheme = "dark";
                options.Theme.Radius = "round";
                options.Theme.Density = "compact";
                options.Header.TitleText = "Assistant";
                options.StartScreen.Greeting = "How can I help?";
                options.StartScreen.Prompts.Add(new ChatKitStartPrompt
                {
                    Label = "Summarize",
                    Prompt = "Summarize the latest contract changes.",
                    Icon = "document",
                });
            })
            .BuildServiceProvider();

        IncursaChatKitTagHelper tagHelper = CreateTagHelper(services);
        tagHelper.Id = "workspace-assistant";
        tagHelper.Class = "chatkit-page";
        tagHelper.SessionEndpoint = "/api/chatkit/session";
        tagHelper.ActionEndpoint = "/api/chatkit/action";
        tagHelper.Placeholder = "Ask the assistant";
        tagHelper.FeedbackEnabled = true;
        tagHelper.RetryEnabled = true;

        TagHelperOutput output = CreateOutput();
        await tagHelper.ProcessAsync(CreateContext(), output);

        Assert.Equal("workspace-assistant", output.Attributes["id"]?.Value);
        Assert.Contains("chatkit-page", output.Attributes["class"]?.Value?.ToString(), StringComparison.Ordinal);
        Assert.Contains("840px", output.Attributes["style"]?.Value?.ToString(), StringComparison.Ordinal);

        JsonNode config = ParseConfig(output);
        Assert.Equal("en", config["locale"]?.GetValue<string>());
        Assert.Equal("Workspace assistant", config["frameTitle"]?.GetValue<string>());
        Assert.Equal("/api/chatkit/session", config["sessionEndpoint"]?.GetValue<string>());
        Assert.Equal("/api/chatkit/action", config["actionEndpoint"]?.GetValue<string>());
        Assert.Equal("dark", config["theme"]?["colorScheme"]?.GetValue<string>());
        Assert.Equal("round", config["theme"]?["radius"]?.GetValue<string>());
        Assert.Equal("compact", config["theme"]?["density"]?.GetValue<string>());
        Assert.Equal("Assistant", config["header"]?["title"]?["text"]?.GetValue<string>());
        Assert.Equal("How can I help?", config["startScreen"]?["greeting"]?.GetValue<string>());
        Assert.Equal("Summarize", config["startScreen"]?["prompts"]?[0]?["label"]?.GetValue<string>());
        Assert.Equal("Ask the assistant", config["composer"]?["placeholder"]?.GetValue<string>());
        Assert.True(config["threadItemActions"]?["feedback"]?.GetValue<bool>());
        Assert.True(config["threadItemActions"]?["retry"]?.GetValue<bool>());
        Assert.True(config["widgetActions"]?["forwardToEndpoint"]?.GetValue<bool>());
    }

    /// <summary>The host tag helper omits the action endpoint when widget forwarding is disabled.</summary>
    /// <intent>Protect the public Razor wrapper surface added to the ASP.NET Core package.</intent>
    /// <scenario>LIB-CHATKIT-ASPNETCORE-003</scenario>
    /// <behavior>Disabling widget forwarding removes the action endpoint from the serialized config.</behavior>
    [Fact]
    public async Task ProcessAsync_DoesNotEmitActionEndpoint_WhenWidgetForwardingDisabled()
    {
        ServiceProvider services = new ServiceCollection()
            .AddLogging()
            .AddIncursaOpenAIChatKitAspNetCore()
            .BuildServiceProvider();

        IncursaChatKitTagHelper tagHelper = CreateTagHelper(services);
        tagHelper.SessionEndpoint = "/api/chatkit/session";
        tagHelper.ForwardWidgetActions = false;

        TagHelperOutput output = CreateOutput();
        await tagHelper.ProcessAsync(CreateContext(), output);

        JsonNode config = ParseConfig(output);
        Assert.Null(config["actionEndpoint"]);
        Assert.False(config["widgetActions"]?["forwardToEndpoint"]?.GetValue<bool>());
    }

    /// <summary>The host tag helper allows hosted API mode to omit a domain key when none is configured.</summary>
    /// <intent>Protect parity with the upstream ChatKit client setup, which allows local development without a domain key.</intent>
    /// <scenario>LIB-CHATKIT-ASPNETCORE-003</scenario>
    /// <behavior>Hosted API mode serializes the API URL without forcing a domain key or local fallback endpoints.</behavior>
    [Fact]
    public async Task ProcessAsync_OmitsDomainKey_WhenHostedApiModeDoesNotConfigureOne()
    {
        ServiceProvider services = new ServiceCollection()
            .AddLogging()
            .AddIncursaOpenAIChatKitAspNetCore()
            .BuildServiceProvider();

        IncursaChatKitTagHelper tagHelper = CreateTagHelper(services);
        tagHelper.ApiUrl = "https://example.contoso.com/chatkit";

        TagHelperOutput output = CreateOutput();
        await tagHelper.ProcessAsync(CreateContext(), output);

        JsonNode config = ParseConfig(output);
        Assert.Equal("https://example.contoso.com/chatkit", config["apiUrl"]?.GetValue<string>());
        Assert.Null(config["domainKey"]);
        Assert.Null(config["sessionEndpoint"]);
        Assert.Null(config["actionEndpoint"]);
    }

    /// <summary>The host tag helper forwards the configured domain key in hosted API mode.</summary>
    /// <intent>Protect the hosted ChatKit browser configuration surface exposed by the ASP.NET Core wrapper.</intent>
    /// <scenario>LIB-CHATKIT-ASPNETCORE-003</scenario>
    /// <behavior>Configured hosted API defaults populate both the API URL and domain key in the serialized host config.</behavior>
    [Fact]
    public async Task ProcessAsync_UsesConfiguredDomainKey_WhenHostedApiModeIsConfigured()
    {
        ServiceProvider services = new ServiceCollection()
            .AddLogging()
            .AddIncursaOpenAIChatKitAspNetCore(options =>
            {
                options.ApiUrl = "https://example.contoso.com/chatkit";
                options.DomainKey = "contoso-domain-key";
            })
            .BuildServiceProvider();

        IncursaChatKitTagHelper tagHelper = CreateTagHelper(services);

        TagHelperOutput output = CreateOutput();
        await tagHelper.ProcessAsync(CreateContext(), output);

        JsonNode config = ParseConfig(output);
        Assert.Equal("https://example.contoso.com/chatkit", config["apiUrl"]?.GetValue<string>());
        Assert.Equal("contoso-domain-key", config["domainKey"]?.GetValue<string>());
        Assert.Null(config["sessionEndpoint"]);
        Assert.Null(config["actionEndpoint"]);
    }

    private static TagHelperContext CreateContext()
    {
        return new TagHelperContext(
            tagName: "incursa-chatkit",
            allAttributes: new TagHelperAttributeList(),
            items: new Dictionary<object, object>(),
            uniqueId: Guid.NewGuid().ToString("N"));
    }

    private static TagHelperOutput CreateOutput()
    {
        return new TagHelperOutput(
            "incursa-chatkit",
            attributes: [],
            getChildContentAsync: (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
    }

    private static IncursaChatKitTagHelper CreateTagHelper(IServiceProvider services)
    {
        return new IncursaChatKitTagHelper(services, NullLogger<IncursaChatKitTagHelper>.Instance)
        {
            ViewContext = new ViewContext
            {
                HttpContext = new DefaultHttpContext
                {
                    RequestServices = services,
                },
                ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
            },
        };
    }

    private static JsonNode ParseConfig(TagHelperOutput output)
    {
        string serializedConfig = output.Attributes["data-incursa-chatkit-config"]!.Value!.ToString()!;
        return JsonNode.Parse(serializedConfig)!;
    }
}
