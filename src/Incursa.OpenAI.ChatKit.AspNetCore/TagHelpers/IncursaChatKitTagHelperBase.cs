using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Incursa.OpenAI.ChatKit.AspNetCore.TagHelpers;

/// <summary>
/// Provides shared rendering behavior for ChatKit Razor tag helpers that emit browser host elements.
/// </summary>
public abstract class IncursaChatKitTagHelperBase : TagHelper
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IServiceProvider serviceProvider;
    private readonly ILogger logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="IncursaChatKitTagHelperBase" /> class.
    /// </summary>
    /// <param name="serviceProvider">The application service provider.</param>
    /// <param name="logger">The logger used for rendering failures.</param>
    protected IncursaChatKitTagHelperBase(
        IServiceProvider serviceProvider,
        ILogger logger)
    {
        this.serviceProvider = serviceProvider;
        this.logger = logger;
    }

    /// <summary>
    /// Gets or sets the rendered element identifier.
    /// </summary>
    [HtmlAttributeName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets additional CSS classes for the rendered host element.
    /// </summary>
    [HtmlAttributeName("class")]
    public string? Class { get; set; }

    /// <summary>
    /// Gets or sets the rendered host height.
    /// </summary>
    [HtmlAttributeName("height")]
    public string? Height { get; set; }

    /// <summary>
    /// Gets or sets the current Razor view context.
    /// </summary>
    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = default!;

    /// <inheritdoc />
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;

        try
        {
            ChatKitHostClientConfig config = BuildClientConfig();

            output.Attributes.SetAttribute("data-incursa-chatkit-host", "true");
            output.Attributes.SetAttribute("data-incursa-chatkit-config", JsonSerializer.Serialize(config, SerializerOptions));
            MergeClass(output, "incursa-chatkit-host");
            MergeClass(output, Class);
            SetIfPresent(output, "id", Id);

            if (!string.IsNullOrWhiteSpace(config.Height))
            {
                AppendStyle(output, $"min-height: {config.Height}; height: {config.Height};");
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to render ChatKit host.");
            output.Attributes.SetAttribute("data-incursa-chatkit-error", "true");
            output.Content.SetContent("ChatKit failed to initialize.");
        }
    }

    internal abstract ChatKitHostClientConfig BuildClientConfig();

    /// <summary>
    /// Resolves the configured ChatKit ASP.NET Core options.
    /// </summary>
    /// <returns>The resolved <see cref="ChatKitAspNetCoreOptions" /> instance.</returns>
    protected ChatKitAspNetCoreOptions ResolveUiOptions()
    {
        return ResolveServices().GetService(typeof(IOptions<ChatKitAspNetCoreOptions>)) is IOptions<ChatKitAspNetCoreOptions> options
            ? options.Value
            : new ChatKitAspNetCoreOptions();
    }

    /// <summary>
    /// Resolves the hosted ChatKit API URL for the current tag helper instance.
    /// </summary>
    /// <param name="apiUrl">The explicit tag helper value.</param>
    /// <returns>The explicit value, the configured default, or <see langword="null" />.</returns>
    protected string? ResolveApiUrl(string? apiUrl)
    {
        if (!string.IsNullOrWhiteSpace(apiUrl))
        {
            return apiUrl;
        }

        ChatKitAspNetCoreOptions uiOptions = ResolveUiOptions();
        return string.IsNullOrWhiteSpace(uiOptions.ApiUrl) ? null : uiOptions.ApiUrl;
    }

    /// <summary>
    /// Resolves the ChatKit domain key for the current tag helper instance.
    /// </summary>
    /// <param name="domainKey">The explicit tag helper value.</param>
    /// <returns>The explicit value, the configured default, or <see langword="null" />.</returns>
    protected string? ResolveDomainKey(string? domainKey)
    {
        if (!string.IsNullOrWhiteSpace(domainKey))
        {
            return domainKey;
        }

        ChatKitAspNetCoreOptions uiOptions = ResolveUiOptions();
        return string.IsNullOrWhiteSpace(uiOptions.DomainKey) ? null : uiOptions.DomainKey;
    }

    /// <summary>
    /// Resolves the session endpoint for the current tag helper instance.
    /// </summary>
    /// <param name="sessionEndpoint">The explicit tag helper value.</param>
    /// <returns>The explicit value, the configured default, or the conventional local session endpoint.</returns>
    protected string ResolveSessionEndpoint(string? sessionEndpoint)
    {
        if (!string.IsNullOrWhiteSpace(sessionEndpoint))
        {
            return sessionEndpoint;
        }

        ChatKitAspNetCoreOptions uiOptions = ResolveUiOptions();
        return !string.IsNullOrWhiteSpace(uiOptions.SessionEndpoint)
            ? uiOptions.SessionEndpoint
            : "/api/chatkit/session";
    }

    /// <summary>
    /// Resolves the widget action endpoint for the current tag helper instance.
    /// </summary>
    /// <param name="actionEndpoint">The explicit tag helper value.</param>
    /// <param name="forwardWidgetActions">Indicates whether widget action forwarding is enabled.</param>
    /// <returns>The explicit value, the configured default, the conventional local action endpoint, or <see langword="null" /> when forwarding is disabled.</returns>
    protected string? ResolveActionEndpoint(string? actionEndpoint, bool forwardWidgetActions)
    {
        if (!forwardWidgetActions)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(actionEndpoint))
        {
            return actionEndpoint;
        }

        ChatKitAspNetCoreOptions uiOptions = ResolveUiOptions();
        return !string.IsNullOrWhiteSpace(uiOptions.ActionEndpoint)
            ? uiOptions.ActionEndpoint
            : "/api/chatkit/action";
    }

    /// <summary>
    /// Resolves the rendered host height for the current tag helper instance.
    /// </summary>
    /// <param name="height">The explicit tag helper value.</param>
    /// <returns>The explicit value or the configured default height.</returns>
    protected string ResolveHeight(string? height)
    {
        return string.IsNullOrWhiteSpace(height)
            ? ResolveUiOptions().DefaultHeight
            : height;
    }

    /// <summary>
    /// Resolves the current request service provider.
    /// </summary>
    /// <returns>The request service provider when a view context is available; otherwise the constructor service provider.</returns>
    protected IServiceProvider ResolveServices()
    {
        return ViewContext?.HttpContext?.RequestServices ?? serviceProvider;
    }

    private static void AppendStyle(TagHelperOutput output, string style)
    {
        if (output.Attributes.TryGetAttribute("style", out TagHelperAttribute? existing))
        {
            output.Attributes.SetAttribute("style", $"{existing.Value}; {style}");
            return;
        }

        output.Attributes.SetAttribute("style", style);
    }

    private static void MergeClass(TagHelperOutput output, string? cssClass)
    {
        if (string.IsNullOrWhiteSpace(cssClass))
        {
            return;
        }

        if (output.Attributes.TryGetAttribute("class", out TagHelperAttribute? existing))
        {
            output.Attributes.SetAttribute("class", $"{existing.Value} {cssClass}".Trim());
            return;
        }

        output.Attributes.SetAttribute("class", cssClass);
    }

    private static void SetIfPresent(TagHelperOutput output, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            output.Attributes.SetAttribute(name, value);
        }
    }
}
