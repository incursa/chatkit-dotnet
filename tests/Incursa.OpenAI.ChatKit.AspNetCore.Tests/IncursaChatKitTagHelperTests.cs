using System.Text.Json.Nodes;
using Incursa.OpenAI.ChatKit;
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
    /// <summary>The generic ChatKit tag helper now requires an explicit mode choice.</summary>
    /// <intent>Prevent ambiguous Razor configuration that silently switches between local and API-backed modes.</intent>
    /// <scenario>LIB-CHATKIT-ASPNETCORE-003</scenario>
    /// <behavior>Using the generic host tag helper emits a render error instead of inferring a mode.</behavior>
    [Fact]
    public async Task ProcessAsync_EmitsError_WhenGenericTagHelperIsUsed()
    {
        ServiceProvider services = new ServiceCollection()
            .AddLogging()
            .AddOpenAIChatKit()
            .BuildServiceProvider();

        IncursaChatKitTagHelper tagHelper = CreateGenericTagHelper(services);
        TagHelperOutput output = CreateOutput();

        await tagHelper.ProcessAsync(CreateContext(), output);

        Assert.Equal("div", output.TagName);
        Assert.Equal("true", output.Attributes["data-incursa-chatkit-error"]?.Value);
        Assert.Null(output.Attributes["data-incursa-chatkit-config"]);
    }

    /// <summary>The hosted ChatKit tag helper serializes explicit hosted session and action endpoints into the browser config payload.</summary>
    /// <intent>Protect the explicit OpenAI-hosted wrapper surface.</intent>
    /// <scenario>LIB-CHATKIT-ASPNETCORE-003</scenario>
    /// <behavior>The hosted host tag helper emits the provided session and action endpoints and omits custom API settings.</behavior>
    [Fact]
    public async Task ProcessAsync_HostedTagHelperSerializesExplicitHostedEndpoints()
    {
        ServiceProvider services = new ServiceCollection()
            .AddLogging()
            .AddOpenAIChatKitHosted()
            .BuildServiceProvider();

        IncursaChatKitHostedTagHelper tagHelper = CreateHostedTagHelper(services);
        tagHelper.SessionEndpoint = "/api/chatkit/session";
        tagHelper.ActionEndpoint = "/api/chatkit/action";

        TagHelperOutput output = CreateOutput();

        await tagHelper.ProcessAsync(CreateContext(), output);

        JsonNode config = ParseConfig(output);
        Assert.Equal("/api/chatkit/session", config["sessionEndpoint"]?.GetValue<string>());
        Assert.Equal("/api/chatkit/action", config["actionEndpoint"]?.GetValue<string>());
        Assert.Null(config["apiUrl"]);
        Assert.Null(config["domainKey"]);
        Assert.Equal("720px", config["height"]?.GetValue<string>());
        Assert.Null(config["entityHandlers"]);
        Assert.Null(config["entities"]);
        Assert.Null(config["disclaimer"]);
    }

    /// <summary>The hosted host tag helper serializes configured UI options and explicit attributes into the browser config payload.</summary>
    /// <intent>Protect the public Razor wrapper surface added to the ASP.NET Core package.</intent>
    /// <scenario>LIB-CHATKIT-ASPNETCORE-003</scenario>
    /// <behavior>Configured options and explicit hosted tag helper attributes appear in the serialized host config.</behavior>
    [Fact]
    public async Task ProcessAsync_HostedTagHelperSerializesConfiguredUiOptions()
    {
        ServiceProvider services = new ServiceCollection()
            .AddLogging()
            .AddOpenAIChatKitHosted(options =>
            {
                options.DefaultHeight = "840px";
                options.Locale = "en";
                options.FrameTitle = "Workspace assistant";
                options.ClientToolHandlers = "window.defaultClientTools";
                options.EntityHandlers = "window.defaultEntityHandlers";
                options.WidgetActionHandler = "window.defaultWidgetActionHandler";
                options.Theme.ColorScheme = "dark";
                options.Theme.Radius = "round";
                options.Theme.Density = "compact";
                options.Theme.Typography.BaseSize = 16;
                options.Theme.Typography.FontFamily = "Inter";
                options.Theme.Typography.FontFamilyMono = "IBM Plex Mono";
                options.Theme.Typography.FontSources.Add(new ChatKitFontSource
                {
                    Family = "Inter",
                    Src = "/fonts/inter.woff2",
                    Display = "swap",
                });
                options.Theme.Color.Grayscale.Hue = 210;
                options.Theme.Color.Grayscale.Tint = 4;
                options.Theme.Color.Grayscale.Shade = -2;
                options.Theme.Color.Accent.Primary = "#8B5CF6";
                options.Theme.Color.Accent.Level = 2;
                options.Theme.Color.Surface.Background = "#111111";
                options.Theme.Color.Surface.Foreground = "#F5F5F5";
                options.Header.LeftAction = new ChatKitHeaderActionOptions
                {
                    Icon = "sidebar-left",
                    OnClickHandler = "window.chatkit.onHeaderAction",
                };
                options.Header.TitleEnabled = true;
                options.Header.TitleText = "Assistant";
                options.StartScreen.Greeting = "How can I help?";
                options.StartScreen.Prompts.Add(new ChatKitStartPrompt
                {
                    Label = "Summarize",
                    Prompt = new UserMessageContent[]
                    {
                        new UserMessageTextContent
                        {
                            Text = "Summarize the latest contract changes.",
                        },
                        new UserMessageTagContent
                        {
                            Id = "doc-1",
                            Text = "Q2 Planning Doc",
                            Data = new Dictionary<string, JsonNode?>
                            {
                                ["source"] = JsonValue.Create("document"),
                            },
                        },
                    },
                    Icon = "document",
                });
                options.Composer.Attachments.Enabled = true;
                options.Composer.Attachments.MaxSize = 4096;
                options.Composer.Attachments.MaxCount = 10;
                options.Composer.Attachments.Accept = new Dictionary<string, string[]>
                {
                    ["application/pdf"] = [".pdf"],
                };
                options.Composer.Tools.Add(new ChatKitComposerTool
                {
                    Id = "summarize",
                    Label = "Summarize",
                    Icon = "book-open",
                    PlaceholderOverride = "Summarize the latest contract changes.",
                });
                options.Composer.Models.Add(new ChatKitComposerModel
                {
                    Id = "gpt-4.1",
                    Label = "Quality",
                    Description = "All rounder",
                    Default = true,
                });
                options.Composer.Dictation.Enabled = true;
                options.Disclaimer.Text = "AI may make mistakes. Verify important details.";
                options.Disclaimer.HighContrast = false;
                options.Entities.ShowComposerMenu = false;
            })
            .BuildServiceProvider();

        IncursaChatKitHostedTagHelper tagHelper = CreateHostedTagHelper(services);
        tagHelper.Id = "workspace-assistant";
        tagHelper.Class = "chatkit-page";
        tagHelper.SessionEndpoint = "/api/chatkit/session";
        tagHelper.ActionEndpoint = "/api/chatkit/action";
        tagHelper.ClientToolHandlers = "app.chatkit.clientTools";
        tagHelper.EntityHandlers = "app.chatkit.entityHandlers";
        tagHelper.WidgetActionHandler = "app.chatkit.onWidgetAction";
        tagHelper.ThemeBaseSize = 18;
        tagHelper.ThemeFontFamily = "Georgia";
        tagHelper.ThemeColorAccentPrimary = "#F97316";
        tagHelper.HeaderLeftActionIcon = "sidebar-right";
        tagHelper.HeaderLeftActionHandler = "app.chatkit.onHeaderAction";
        tagHelper.HeaderTitleEnabled = false;
        tagHelper.Placeholder = "Ask the assistant";
        tagHelper.DisclaimerText = "Review important details before taking action.";
        tagHelper.DisclaimerHighContrast = true;
        tagHelper.EntityShowComposerMenu = true;
        tagHelper.ComposerAttachmentsMaxSize = 2048;
        tagHelper.ComposerAttachmentsMaxCount = 3;
        tagHelper.ComposerDictationEnabled = false;
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
        Assert.Equal("app.chatkit.clientTools", config["clientToolHandlers"]?.GetValue<string>());
        Assert.Equal("app.chatkit.entityHandlers", config["entityHandlers"]?.GetValue<string>());
        Assert.Equal("app.chatkit.onWidgetAction", config["widgetActionHandler"]?.GetValue<string>());
        Assert.Equal("dark", config["theme"]?["colorScheme"]?.GetValue<string>());
        Assert.Equal(18, config["theme"]?["typography"]?["baseSize"]?.GetValue<int>());
        Assert.Equal("Georgia", config["theme"]?["typography"]?["fontFamily"]?.GetValue<string>());
        Assert.Equal("IBM Plex Mono", config["theme"]?["typography"]?["fontFamilyMono"]?.GetValue<string>());
        Assert.Equal("Inter", config["theme"]?["typography"]?["fontSources"]?[0]?["family"]?.GetValue<string>());
        Assert.Equal(210, config["theme"]?["color"]?["grayscale"]?["hue"]?.GetValue<int>());
        Assert.Equal("#F97316", config["theme"]?["color"]?["accent"]?["primary"]?.GetValue<string>());
        Assert.Equal("#111111", config["theme"]?["color"]?["surface"]?["background"]?.GetValue<string>());
        Assert.Equal("sidebar-right", config["header"]?["leftAction"]?["icon"]?.GetValue<string>());
        Assert.Equal("app.chatkit.onHeaderAction", config["header"]?["leftAction"]?["onClickHandler"]?.GetValue<string>());
        Assert.Equal("round", config["theme"]?["radius"]?.GetValue<string>());
        Assert.Equal("compact", config["theme"]?["density"]?.GetValue<string>());
        Assert.False(config["header"]?["title"]?["enabled"]?.GetValue<bool>());
        Assert.Equal("Assistant", config["header"]?["title"]?["text"]?.GetValue<string>());
        Assert.Equal("How can I help?", config["startScreen"]?["greeting"]?.GetValue<string>());
        Assert.Equal("Summarize", config["startScreen"]?["prompts"]?[0]?["label"]?.GetValue<string>());
        Assert.Equal("input_text", config["startScreen"]?["prompts"]?[0]?["prompt"]?[0]?["type"]?.GetValue<string>());
        Assert.Equal("input_tag", config["startScreen"]?["prompts"]?[0]?["prompt"]?[1]?["type"]?.GetValue<string>());
        Assert.Equal("doc-1", config["startScreen"]?["prompts"]?[0]?["prompt"]?[1]?["id"]?.GetValue<string>());
        Assert.Equal("Ask the assistant", config["composer"]?["placeholder"]?.GetValue<string>());
        Assert.True(config["composer"]?["attachments"]?["enabled"]?.GetValue<bool>());
        Assert.Equal(2048, config["composer"]?["attachments"]?["maxSize"]?.GetValue<long>());
        Assert.Equal(3, config["composer"]?["attachments"]?["maxCount"]?.GetValue<int>());
        Assert.Equal(".pdf", config["composer"]?["attachments"]?["accept"]?["application/pdf"]?[0]?.GetValue<string>());
        Assert.Equal("summarize", config["composer"]?["tools"]?[0]?["id"]?.GetValue<string>());
        Assert.Equal("Quality", config["composer"]?["models"]?[0]?["label"]?.GetValue<string>());
        Assert.False(config["composer"]?["dictation"]?["enabled"]?.GetValue<bool>());
        Assert.Equal("Review important details before taking action.", config["disclaimer"]?["text"]?.GetValue<string>());
        Assert.True(config["disclaimer"]?["highContrast"]?.GetValue<bool>());
        Assert.True(config["entities"]?["showComposerMenu"]?.GetValue<bool>());
        Assert.True(config["threadItemActions"]?["feedback"]?.GetValue<bool>());
        Assert.True(config["threadItemActions"]?["retry"]?.GetValue<bool>());
        Assert.True(config["widgetActions"]?["forwardToEndpoint"]?.GetValue<bool>());
    }

    /// <summary>The API host tag helper serializes composer options and upload strategy metadata into the browser config payload.</summary>
    /// <intent>Protect the packaged API-mode projection for attachments, composer pickers, dictation, and upload strategy.</intent>
    /// <scenario>LIB-CHATKIT-ASPNETCORE-003</scenario>
    /// <behavior>Configured API-mode composer defaults and explicit upload strategy settings appear in the serialized host config.</behavior>
    [Fact]
    public async Task ProcessAsync_ApiTagHelperSerializesComposerConfigAndUploadStrategy()
    {
        ServiceProvider services = new ServiceCollection()
            .AddLogging()
            .AddOpenAIChatKitApi(
                "https://example.contoso.com/chatkit",
                "contoso-domain-key",
                options =>
                {
                    options.Composer.Attachments.Enabled = true;
                    options.Composer.Attachments.MaxCount = 5;
                    options.Composer.Attachments.MaxSize = 2048;
                    options.Composer.Attachments.Accept = new Dictionary<string, string[]>
                    {
                        ["image/*"] = [".png", ".jpg"],
                    };
                    options.Composer.Tools.Add(new ChatKitComposerTool
                    {
                        Id = "translate",
                        Label = "Translate",
                        Icon = "globe",
                    });
                    options.Composer.Models.Add(new ChatKitComposerModel
                    {
                        Id = "gpt-4.1-mini",
                        Label = "Fast",
                        Description = "Answers right away",
                    });
                    options.Composer.Dictation.Enabled = true;
                    options.UploadStrategy.Type = "direct";
                    options.UploadStrategy.UploadUrl = "/files";
                })
            .BuildServiceProvider();

        IncursaChatKitApiTagHelper tagHelper = CreateApiTagHelper(services);
        tagHelper.ApiUrl = "https://example.contoso.com/chatkit";
        tagHelper.UploadStrategyType = "two_phase";
        tagHelper.ComposerAttachmentsEnabled = true;
        tagHelper.ComposerAttachmentsMaxCount = 4;
        tagHelper.ComposerDictationEnabled = true;

        TagHelperOutput output = CreateOutput();
        await tagHelper.ProcessAsync(CreateContext(), output);

        JsonNode config = ParseConfig(output);
        Assert.Equal("two_phase", config["uploadStrategy"]?["type"]?.GetValue<string>());
        Assert.Null(config["uploadStrategy"]?["uploadUrl"]);
        Assert.True(config["composer"]?["attachments"]?["enabled"]?.GetValue<bool>());
        Assert.Equal(2048, config["composer"]?["attachments"]?["maxSize"]?.GetValue<long>());
        Assert.Equal(4, config["composer"]?["attachments"]?["maxCount"]?.GetValue<int>());
        Assert.Equal(".png", config["composer"]?["attachments"]?["accept"]?["image/*"]?[0]?.GetValue<string>());
        Assert.Equal("translate", config["composer"]?["tools"]?[0]?["id"]?.GetValue<string>());
        Assert.Equal("gpt-4.1-mini", config["composer"]?["models"]?[0]?["id"]?.GetValue<string>());
        Assert.True(config["composer"]?["dictation"]?["enabled"]?.GetValue<bool>());
    }

    /// <summary>The hosted host tag helper omits disclaimer settings when no disclaimer text is configured.</summary>
    /// <intent>Preserve the upstream disclaimer contract, which requires text when disclaimer settings are sent.</intent>
    /// <scenario>LIB-CHATKIT-ASPNETCORE-003</scenario>
    /// <behavior>High-contrast defaults alone do not serialize a disclaimer object into the browser config.</behavior>
    [Fact]
    public async Task ProcessAsync_HostedTagHelperOmitsDisclaimer_WhenTextIsNotConfigured()
    {
        ServiceProvider services = new ServiceCollection()
            .AddLogging()
            .AddOpenAIChatKitHosted(options =>
            {
                options.Disclaimer.HighContrast = true;
            })
            .BuildServiceProvider();

        IncursaChatKitHostedTagHelper tagHelper = CreateHostedTagHelper(services);
        tagHelper.SessionEndpoint = "/api/chatkit/session";
        tagHelper.ActionEndpoint = "/api/chatkit/action";

        TagHelperOutput output = CreateOutput();
        await tagHelper.ProcessAsync(CreateContext(), output);

        JsonNode config = ParseConfig(output);
        Assert.Null(config["disclaimer"]);
    }

    /// <summary>The hosted host tag helper omits the action endpoint when widget forwarding is disabled.</summary>
    /// <intent>Protect the explicit hosted Razor wrapper surface added to the ASP.NET Core package.</intent>
    /// <scenario>LIB-CHATKIT-ASPNETCORE-003</scenario>
    /// <behavior>Disabling widget forwarding removes the action endpoint from the serialized config.</behavior>
    [Fact]
    public async Task ProcessAsync_HostedTagHelperDoesNotEmitActionEndpoint_WhenWidgetForwardingDisabled()
    {
        ServiceProvider services = new ServiceCollection()
            .AddLogging()
            .AddOpenAIChatKitHosted()
            .BuildServiceProvider();

        IncursaChatKitHostedTagHelper tagHelper = CreateHostedTagHelper(services);
        tagHelper.SessionEndpoint = "/api/chatkit/session";
        tagHelper.ForwardWidgetActions = false;

        TagHelperOutput output = CreateOutput();
        await tagHelper.ProcessAsync(CreateContext(), output);

        JsonNode config = ParseConfig(output);
        Assert.Null(config["actionEndpoint"]);
        Assert.False(config["widgetActions"]?["forwardToEndpoint"]?.GetValue<bool>());
    }

    /// <summary>The hosted host tag helper supports client-only widget callbacks without requiring a forwarding endpoint.</summary>
    /// <intent>Protect the upstream widgets.onAction parity surface in hosted mode.</intent>
    /// <scenario>LIB-CHATKIT-ASPNETCORE-003</scenario>
    /// <behavior>Registering a widget action callback while disabling forwarding preserves the client handler and omits the action endpoint.</behavior>
    [Fact]
    public async Task ProcessAsync_HostedTagHelperSerializesClientOnlyWidgetActionHandler()
    {
        ServiceProvider services = new ServiceCollection()
            .AddLogging()
            .AddOpenAIChatKitHosted()
            .BuildServiceProvider();

        IncursaChatKitHostedTagHelper tagHelper = CreateHostedTagHelper(services);
        tagHelper.SessionEndpoint = "/api/chatkit/session";
        tagHelper.WidgetActionHandler = "window.chatkit.onWidgetAction";
        tagHelper.ForwardWidgetActions = false;

        TagHelperOutput output = CreateOutput();
        await tagHelper.ProcessAsync(CreateContext(), output);

        JsonNode config = ParseConfig(output);
        Assert.Equal("window.chatkit.onWidgetAction", config["widgetActionHandler"]?.GetValue<string>());
        Assert.Null(config["actionEndpoint"]);
        Assert.False(config["widgetActions"]?["forwardToEndpoint"]?.GetValue<bool>());
    }

    /// <summary>The hosted host tag helper serializes both a client handler and endpoint forwarding when both are configured.</summary>
    /// <intent>Protect the coexistence contract where client handling and server forwarding are both active.</intent>
    /// <scenario>LIB-CHATKIT-ASPNETCORE-003</scenario>
    /// <behavior>When a widget action handler and forward-widget-actions are both configured, both are emitted so the runtime can run client handling first, then forward to the endpoint.</behavior>
    [Fact]
    public async Task ProcessAsync_HostedTagHelperSerializesCoexistenceOfHandlerAndForwarding()
    {
        ServiceProvider services = new ServiceCollection()
            .AddLogging()
            .AddOpenAIChatKitHosted()
            .BuildServiceProvider();

        IncursaChatKitHostedTagHelper tagHelper = CreateHostedTagHelper(services);
        tagHelper.SessionEndpoint = "/api/chatkit/session";
        tagHelper.ActionEndpoint = "/api/chatkit/action";
        tagHelper.WidgetActionHandler = "window.chatkit.onWidgetAction";
        tagHelper.ForwardWidgetActions = true;

        TagHelperOutput output = CreateOutput();
        await tagHelper.ProcessAsync(CreateContext(), output);

        JsonNode config = ParseConfig(output);
        Assert.Equal("window.chatkit.onWidgetAction", config["widgetActionHandler"]?.GetValue<string>());
        Assert.Equal("/api/chatkit/action", config["actionEndpoint"]?.GetValue<string>());
        Assert.True(config["widgetActions"]?["forwardToEndpoint"]?.GetValue<bool>());
    }

    /// <summary>The API host tag helper emits an error when the direct browser domain key is missing.</summary>
    /// <intent>Protect the direct browser wrapper from serializing an invalid ChatKit API configuration.</intent>
    /// <scenario>LIB-CHATKIT-ASPNETCORE-003</scenario>
    /// <behavior>Direct browser API mode emits a render error instead of serializing a config payload when the domain key is missing.</behavior>
    [Fact]
    public async Task ProcessAsync_ApiTagHelperEmitsError_WhenDomainKeyIsNotConfigured()
    {
        ServiceProvider services = new ServiceCollection()
            .AddLogging()
            .AddOpenAIChatKit()
            .BuildServiceProvider();

        IncursaChatKitApiTagHelper tagHelper = CreateApiTagHelper(services);
        tagHelper.ApiUrl = "https://example.contoso.com/chatkit";

        TagHelperOutput output = CreateOutput();
        await tagHelper.ProcessAsync(CreateContext(), output);

        Assert.Equal("true", output.Attributes["data-incursa-chatkit-error"]?.Value);
        Assert.Null(output.Attributes["data-incursa-chatkit-config"]);
    }

    /// <summary>The API host tag helper forwards the configured domain key in direct browser API mode.</summary>
    /// <intent>Protect the direct browser ChatKit configuration surface exposed by the ASP.NET Core wrapper.</intent>
    /// <scenario>LIB-CHATKIT-ASPNETCORE-003</scenario>
    /// <behavior>Configured direct browser API defaults populate both the API URL and domain key in the serialized host config.</behavior>
    [Fact]
    public async Task ProcessAsync_ApiTagHelperUsesConfiguredDomainKey_WhenConfigured()
    {
        ServiceProvider services = new ServiceCollection()
            .AddLogging()
            .AddOpenAIChatKitApi(
                "https://example.contoso.com/chatkit",
                "contoso-domain-key",
                options =>
                {
                    options.Locale = "en";
                    options.WidgetActionHandler = "window.chatkit.onWidgetAction";
                })
            .BuildServiceProvider();

        IncursaChatKitApiTagHelper tagHelper = CreateApiTagHelper(services);
        tagHelper.ApiUrl = "https://example.contoso.com/chatkit";

        TagHelperOutput output = CreateOutput();
        await tagHelper.ProcessAsync(CreateContext(), output);

        JsonNode config = ParseConfig(output);
        Assert.Equal("https://example.contoso.com/chatkit", config["apiUrl"]?.GetValue<string>());
        Assert.Equal("contoso-domain-key", config["domainKey"]?.GetValue<string>());
        Assert.Null(config["sessionEndpoint"]);
        Assert.Null(config["actionEndpoint"]);
        Assert.Equal("window.chatkit.onWidgetAction", config["widgetActionHandler"]?.GetValue<string>());
        Assert.Equal("en", config["locale"]?.GetValue<string>());
    }

    /// <summary>The hosted host tag helper serializes a client tool handler lookup path set directly on the tag helper.</summary>
    /// <intent>Protect the client tool callback parity surface exposed by the ASP.NET Core Razor wrapper.</intent>
    /// <scenario>LIB-CHATKIT-ASPNETCORE-003</scenario>
    /// <behavior>Setting the client-tool-handlers attribute serializes the lookup path into the browser host config so the runtime can wire onClientTool.</behavior>
    [Fact]
    public async Task ProcessAsync_HostedTagHelperSerializesClientToolHandlers_WhenSetByAttribute()
    {
        ServiceProvider services = new ServiceCollection()
            .AddLogging()
            .AddOpenAIChatKitHosted()
            .BuildServiceProvider();

        IncursaChatKitHostedTagHelper tagHelper = CreateHostedTagHelper(services);
        tagHelper.SessionEndpoint = "/api/chatkit/session";
        tagHelper.ActionEndpoint = "/api/chatkit/action";
        tagHelper.ClientToolHandlers = "window.chatkitClientTools";

        TagHelperOutput output = CreateOutput();
        await tagHelper.ProcessAsync(CreateContext(), output);

        JsonNode config = ParseConfig(output);
        Assert.Equal("window.chatkitClientTools", config["clientToolHandlers"]?.GetValue<string>());
    }

    /// <summary>The hosted host tag helper propagates a default client tool handler lookup path from service options.</summary>
    /// <intent>Protect the server-side options path that wires onClientTool without requiring per-page tag helper attributes.</intent>
    /// <scenario>LIB-CHATKIT-ASPNETCORE-003</scenario>
    /// <behavior>Configuring ClientToolHandlers in AddOpenAIChatKitHosted populates the browser host config with the lookup path.</behavior>
    [Fact]
    public async Task ProcessAsync_HostedTagHelperUsesDefaultClientToolHandlers_WhenConfigured()
    {
        ServiceProvider services = new ServiceCollection()
            .AddLogging()
            .AddOpenAIChatKitHosted(options =>
            {
                options.ClientToolHandlers = "window.defaultClientTools";
            })
            .BuildServiceProvider();

        IncursaChatKitHostedTagHelper tagHelper = CreateHostedTagHelper(services);
        tagHelper.SessionEndpoint = "/api/chatkit/session";
        tagHelper.ActionEndpoint = "/api/chatkit/action";

        TagHelperOutput output = CreateOutput();
        await tagHelper.ProcessAsync(CreateContext(), output);

        JsonNode config = ParseConfig(output);
        Assert.Equal("window.defaultClientTools", config["clientToolHandlers"]?.GetValue<string>());
    }

    /// <summary>The API host tag helper serializes a client tool handler lookup path in direct API mode.</summary>
    /// <intent>Protect the client tool callback parity surface for the direct browser API mode wrapper.</intent>
    /// <scenario>LIB-CHATKIT-ASPNETCORE-003</scenario>
    /// <behavior>Setting client-tool-handlers on the API tag helper serializes the lookup path into the browser host config so the runtime can wire onClientTool.</behavior>
    [Fact]
    public async Task ProcessAsync_ApiTagHelperSerializesClientToolHandlers_WhenSet()
    {
        ServiceProvider services = new ServiceCollection()
            .AddLogging()
            .AddOpenAIChatKitApi(
                "https://example.contoso.com/chatkit",
                "contoso-domain-key")
            .BuildServiceProvider();

        IncursaChatKitApiTagHelper tagHelper = CreateApiTagHelper(services);
        tagHelper.ApiUrl = "https://example.contoso.com/chatkit";
        tagHelper.ClientToolHandlers = "window.chatkitClientTools";

        TagHelperOutput output = CreateOutput();
        await tagHelper.ProcessAsync(CreateContext(), output);

        JsonNode config = ParseConfig(output);
        Assert.Equal("window.chatkitClientTools", config["clientToolHandlers"]?.GetValue<string>());
        Assert.Null(config["sessionEndpoint"]);
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

    private static IncursaChatKitTagHelper CreateGenericTagHelper(IServiceProvider services)
    {
        return new IncursaChatKitTagHelper(services, NullLogger<IncursaChatKitTagHelper>.Instance)
        {
            ViewContext = CreateViewContext(services),
        };
    }

    private static IncursaChatKitHostedTagHelper CreateHostedTagHelper(IServiceProvider services)
    {
        return new IncursaChatKitHostedTagHelper(services, NullLogger<IncursaChatKitHostedTagHelper>.Instance)
        {
            ViewContext = CreateViewContext(services),
        };
    }

    private static IncursaChatKitApiTagHelper CreateApiTagHelper(IServiceProvider services)
    {
        return new IncursaChatKitApiTagHelper(services, NullLogger<IncursaChatKitApiTagHelper>.Instance)
        {
            ViewContext = CreateViewContext(services),
        };
    }

    private static ViewContext CreateViewContext(IServiceProvider services)
    {
        return new ViewContext
        {
            HttpContext = new DefaultHttpContext
            {
                RequestServices = services,
            },
            ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
        };
    }

    private static JsonNode ParseConfig(TagHelperOutput output)
    {
        string serializedConfig = output.Attributes["data-incursa-chatkit-config"]!.Value!.ToString()!;
        return JsonNode.Parse(serializedConfig)!;
    }
}
