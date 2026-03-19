using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Logging;

namespace Incursa.OpenAI.ChatKit.AspNetCore.TagHelpers;

/// <summary>
/// Renders a ChatKit host element that always uses a custom ChatKit API endpoint.
/// </summary>
[HtmlTargetElement("incursa-chatkit-api")]
public sealed class IncursaChatKitApiTagHelper : IncursaChatKitTagHelper
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IncursaChatKitApiTagHelper" /> class.
    /// </summary>
    /// <param name="serviceProvider">The application service provider.</param>
    /// <param name="logger">The logger used for rendering failures.</param>
    public IncursaChatKitApiTagHelper(
        IServiceProvider serviceProvider,
        ILogger<IncursaChatKitApiTagHelper> logger)
        : base(serviceProvider, logger)
    {
    }

    internal override ChatKitHostClientConfig BuildClientConfig()
    {
        if (string.IsNullOrWhiteSpace(ApiUrl))
        {
            throw new InvalidOperationException(
                "The <incursa-chatkit-api> tag helper requires 'api-url'.");
        }

        if (!string.IsNullOrWhiteSpace(SessionEndpoint))
        {
            throw new InvalidOperationException(
                "The <incursa-chatkit-api> tag helper does not support 'session-endpoint'. Use <incursa-chatkit-hosted> for OpenAI-hosted session/action endpoints.");
        }

        if (!string.IsNullOrWhiteSpace(ActionEndpoint))
        {
            throw new InvalidOperationException(
                "The <incursa-chatkit-api> tag helper does not support 'action-endpoint'. Use <incursa-chatkit-hosted> for OpenAI-hosted session/action endpoints.");
        }

        ChatKitHostClientConfig config = BuildFlexibleClientConfig();
        if (string.IsNullOrWhiteSpace(config.DomainKey))
        {
            throw new InvalidOperationException(
                "The <incursa-chatkit-api> tag helper requires 'domain-key'.");
        }

        return new ChatKitHostClientConfig
        {
            ApiUrl = config.ApiUrl,
            DomainKey = config.DomainKey,
            SessionEndpoint = null,
            ActionEndpoint = null,
            Height = config.Height,
            Locale = config.Locale,
            FrameTitle = config.FrameTitle,
            InitialThread = config.InitialThread,
            ClientToolHandlers = config.ClientToolHandlers,
            EntityHandlers = config.EntityHandlers,
            WidgetActionHandler = config.WidgetActionHandler,
            Theme = config.Theme,
            Header = config.Header,
            History = config.History,
            StartScreen = config.StartScreen,
            Composer = config.Composer,
            UploadStrategy = config.UploadStrategy,
            Disclaimer = config.Disclaimer,
            Entities = config.Entities,
            ThreadItemActions = config.ThreadItemActions,
            WidgetActions = config.WidgetActions,
        };
    }
}
